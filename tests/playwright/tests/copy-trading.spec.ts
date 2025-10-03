import { test, expect } from '@playwright/test';
import { CopyTradingHarness, TradeCommand, CopyTradeExecution } from '../src/harness.js';

test.describe.serial('copy trading management flows', () => {
  let harness: CopyTradingHarness;

  test.beforeEach(async () => {
    harness = await CopyTradingHarness.create();
  });

  test.afterEach(async () => {
    await harness.dispose();
  });

  test('scenario 1: single EA order lifecycle', async () => {
    const master = await harness.connectEa('acct-master', 'master-secret');
    await harness.clearOutbox(master);
    await harness.approveSession(master, 'ops-console');
    await harness.assertSessionAuthenticated(master);

    const openCommand: TradeCommand = {
      commandType: 'open',
      instrument: 'USDJPY',
      orderType: 'market',
      side: 'buy',
      volume: 1.0,
      stopLoss: 131.2,
      takeProfit: 132.75,
      timeInForce: 'gtc',
      clientOrderId: 'master-open-1',
      metadata: { source: 'ops-console' },
    };

    await harness.enqueueTradeOrder(master, openCommand);

    const openEvents = await harness.pollOutboxUntil(
      master,
      (events) => events.some((event) => event.eventType === 'OrderCommand'),
    );
    const openOrder = openEvents.find((event) => event.eventType === 'OrderCommand');
    expect(openOrder?.payload['commandType']).toBe('open');
    expect(openOrder?.payload['instrument']).toBe('USDJPY');
    await harness.ackEvents(master, openEvents);

    const closeCommand: TradeCommand = {
      commandType: 'close',
      instrument: 'USDJPY',
      orderType: 'market',
      side: 'sell',
      volume: 1.0,
      timeInForce: 'ioc',
      positionId: 'pos-master-1',
      clientOrderId: 'master-close-1',
      metadata: { source: 'ops-console' },
    };

    await harness.enqueueTradeOrder(master, closeCommand);

    const closeEvents = await harness.pollOutboxUntil(
      master,
      (events) => events.some((event) => event.eventType === 'OrderCommand'),
    );
    const closeOrder = closeEvents.find((event) => event.eventType === 'OrderCommand');
    expect(closeOrder?.payload['commandType']).toBe('close');
    expect(closeOrder?.payload['positionId']).toBe('pos-master-1');
    await harness.ackEvents(master, closeEvents);
  });

  test('scenario 2: dual EA copy trading within one group', async () => {
    const leader = await harness.connectEa('acct-leader', 'leader-secret');
    const follower = await harness.connectEa('acct-follower', 'follower-secret');

    await harness.clearOutbox(leader);
    await harness.clearOutbox(follower);

    await harness.approveSession(leader, 'ops-console');
    await harness.approveSession(follower, 'ops-console');
    await harness.assertSessionAuthenticated(leader);
    await harness.assertSessionAuthenticated(follower);

    await harness.createCopyGroup('swing-alpha', 'Swing Alpha', 'ops-console');
    await harness.upsertGroupMember('swing-alpha', leader, 'leader', 'balanced', 1.0, 'ops-console');
    await harness.upsertGroupMember('swing-alpha', follower, 'follower', 'balanced', 1.0, 'ops-console');

    const membershipEvents = await harness.pollOutboxUntil(
      follower,
      (events) => events.some((event) => event.eventType === 'CopyTradeGroupUpdated'),
    );
    const groupUpdate = membershipEvents.find((event) => event.eventType === 'CopyTradeGroupUpdated');
    expect(groupUpdate?.payload['groupId']).toBe('swing-alpha');
    await harness.ackEvents(follower, membershipEvents);

    const execution: CopyTradeExecution = {
      sourceAccount: leader.account,
      initiatedBy: 'ops-console',
      command: {
        commandType: 'open',
        instrument: 'EURUSD',
        orderType: 'market',
        side: 'buy',
        volume: 0.5,
        stopLoss: 1.0812,
        takeProfit: 1.0975,
        timeInForce: 'gtc',
        clientOrderId: 'copy-order-1',
        metadata: { campaign: 'swing-alpha' },
      },
    };

    await harness.executeCopyTrade('swing-alpha', execution);

    const followerOrders = await harness.pollOutboxUntil(
      follower,
      (events) => events.some((event) => event.eventType === 'OrderCommand'),
    );
    const followerOrder = followerOrders.find((event) => event.eventType === 'OrderCommand');
    expect(followerOrder?.payload['instrument']).toBe('EURUSD');
    expect(followerOrder?.payload['metadata']?.['groupId']).toBe('swing-alpha');
    await harness.ackEvents(follower, followerOrders);
  });

  test('scenario 3: independent orders across multiple groups', async () => {
    const leaderA = await harness.connectEa('acct-alpha-leader', 'alpha-secret');
    const followerA = await harness.connectEa('acct-alpha-follower', 'alpha-follow');
    const leaderB = await harness.connectEa('acct-beta-leader', 'beta-secret');
    const followerB = await harness.connectEa('acct-beta-follower', 'beta-follow');

    for (const session of [leaderA, followerA, leaderB, followerB]) {
      await harness.clearOutbox(session);
      await harness.approveSession(session, 'ops-console');
      await harness.assertSessionAuthenticated(session);
    }

    await harness.createCopyGroup('momentum-alpha', 'Momentum Alpha', 'ops-console');
    await harness.createCopyGroup('momentum-beta', 'Momentum Beta', 'ops-console');

    await harness.upsertGroupMember('momentum-alpha', leaderA, 'leader', 'aggressive', 1.0, 'ops-console');
    await harness.upsertGroupMember('momentum-alpha', followerA, 'follower', 'balanced', 1.0, 'ops-console');
    await harness.upsertGroupMember('momentum-beta', leaderB, 'leader', 'conservative', 1.0, 'ops-console');
    await harness.upsertGroupMember('momentum-beta', followerB, 'follower', 'balanced', 1.0, 'ops-console');

    for (const session of [followerA, followerB]) {
      const updates = await harness.pollOutboxUntil(
        session,
        (events) => events.some((event) => event.eventType === 'CopyTradeGroupUpdated'),
      );
      await harness.ackEvents(session, updates);
    }

    await harness.executeCopyTrade('momentum-alpha', {
      sourceAccount: leaderA.account,
      initiatedBy: 'ops-alpha',
      command: {
        commandType: 'open',
        instrument: 'GBPUSD',
        orderType: 'market',
        side: 'buy',
        volume: 0.8,
        stopLoss: 1.2512,
        takeProfit: 1.2695,
        timeInForce: 'gtc',
        clientOrderId: 'alpha-ord-1',
        metadata: { campaign: 'momentum-alpha' },
      },
    });

    await harness.executeCopyTrade('momentum-beta', {
      sourceAccount: leaderB.account,
      initiatedBy: 'ops-beta',
      command: {
        commandType: 'open',
        instrument: 'AUDUSD',
        orderType: 'market',
        side: 'sell',
        volume: 0.4,
        stopLoss: 0.6652,
        takeProfit: 0.6511,
        timeInForce: 'gtc',
        clientOrderId: 'beta-ord-1',
        metadata: { campaign: 'momentum-beta' },
      },
    });

    const followerAOrders = await harness.pollOutboxUntil(
      followerA,
      (events) => events.some((event) => event.eventType === 'OrderCommand'),
    );
    const orderA = followerAOrders.find((event) => event.eventType === 'OrderCommand');
    expect(orderA?.payload['instrument']).toBe('GBPUSD');
    expect(orderA?.payload['metadata']?.['groupId']).toBe('momentum-alpha');
    await harness.ackEvents(followerA, followerAOrders);

    const followerBOrders = await harness.pollOutboxUntil(
      followerB,
      (events) => events.some((event) => event.eventType === 'OrderCommand'),
    );
    const orderB = followerBOrders.find((event) => event.eventType === 'OrderCommand');
    expect(orderB?.payload['instrument']).toBe('AUDUSD');
    expect(orderB?.payload['metadata']?.['groupId']).toBe('momentum-beta');
    await harness.ackEvents(followerB, followerBOrders);
  });

  test('scenario 4: multi-group EA executes separate orders', async () => {
    const leaderPrimary = await harness.connectEa('acct-swing-leader', 'swing-secret');
    const leaderSecondary = await harness.connectEa('acct-hedge-leader', 'hedge-secret');
    const followerShared = await harness.connectEa('acct-shared-follower', 'shared-secret');

    for (const session of [leaderPrimary, leaderSecondary, followerShared]) {
      await harness.clearOutbox(session);
      await harness.approveSession(session, 'ops-console');
      await harness.assertSessionAuthenticated(session);
    }

    await harness.createCopyGroup('swing-cadre', 'Swing Cadre', 'ops-console');
    await harness.createCopyGroup('hedge-cadre', 'Hedge Cadre', 'ops-console');

    await harness.upsertGroupMember('swing-cadre', leaderPrimary, 'leader', 'balanced', 1.0, 'ops-console');
    await harness.upsertGroupMember('swing-cadre', followerShared, 'follower', 'balanced', 1.0, 'ops-console');
    await harness.upsertGroupMember('hedge-cadre', leaderSecondary, 'leader', 'conservative', 1.0, 'ops-console');
    await harness.upsertGroupMember('hedge-cadre', followerShared, 'follower', 'balanced', 1.0, 'ops-console');

    const membershipSync = await harness.pollOutboxUntil(
      followerShared,
      (events) => events.some((event) => event.eventType === 'CopyTradeGroupUpdated'),
    );
    await harness.ackEvents(followerShared, membershipSync);

    await harness.executeCopyTrade('swing-cadre', {
      sourceAccount: leaderPrimary.account,
      initiatedBy: 'ops-swing',
      command: {
        commandType: 'open',
        instrument: 'NZDJPY',
        orderType: 'market',
        side: 'buy',
        volume: 1.2,
        stopLoss: 88.15,
        takeProfit: 91.65,
        timeInForce: 'gtc',
        clientOrderId: 'swing-order-1',
        metadata: { playbook: 'swing' },
      },
    });

    await harness.executeCopyTrade('hedge-cadre', {
      sourceAccount: leaderSecondary.account,
      initiatedBy: 'ops-hedge',
      command: {
        commandType: 'open',
        instrument: 'USDCHF',
        orderType: 'market',
        side: 'sell',
        volume: 0.6,
        stopLoss: 0.8925,
        takeProfit: 0.8742,
        timeInForce: 'gtc',
        clientOrderId: 'hedge-order-1',
        metadata: { playbook: 'hedge' },
      },
    });

    const sharedOrders = await harness.pollOutboxUntil(
      followerShared,
      (events) =>
        events.filter((event) => event.eventType === 'OrderCommand').length >= 2,
      15,
      300,
    );

    const swingOrder = sharedOrders.find(
      (event) =>
        event.eventType === 'OrderCommand' && event.payload['metadata']?.['groupId'] === 'swing-cadre',
    );
    const hedgeOrder = sharedOrders.find(
      (event) =>
        event.eventType === 'OrderCommand' && event.payload['metadata']?.['groupId'] === 'hedge-cadre',
    );

    expect(swingOrder?.payload['instrument']).toBe('NZDJPY');
    expect(hedgeOrder?.payload['instrument']).toBe('USDCHF');
    await harness.ackEvents(followerShared, sharedOrders);
  });
});
