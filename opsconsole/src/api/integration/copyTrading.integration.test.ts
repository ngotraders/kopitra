import { randomUUID } from 'node:crypto';
import { beforeAll, describe, expect, it } from 'vitest';
import { approveExpertAdvisorSession } from './approveExpertAdvisorSession.ts';
import {
  acknowledgeOutboxEvents,
  clearSessionOutbox,
  createExpertAdvisorSession,
  fetchSessionOutbox,
  type ExpertAdvisorSession,
} from './expertAdvisorSessions.ts';
import {
  executeCopyTradeOrder,
  createCopyTradeGroup,
  getCopyTradeGroup,
  upsertCopyTradeGroupMember,
} from './copyTradeGroups.ts';
import { enqueueExpertAdvisorTradeOrder } from './enqueueExpertAdvisorTradeOrder.ts';
import { gatewayRequest, getIntegrationConfig, managementRequest } from './config.ts';

const RETRY_ATTEMPTS = 20;
const RETRY_DELAY_MS = 250;

describe.sequential('Ops console copy trading integration', () => {
  beforeAll(async () => {
    const config = getIntegrationConfig();
    await waitForGateway();
    await waitForManagement();
    // Log base URLs for easier troubleshooting when running in CI.
    // eslint-disable-next-line no-console
    console.log('Integration targets', {
      management: config.managementBaseUrl,
      gateway: config.gatewayBaseUrl,
    });
  });

  it('approves a single EA session and dispatches direct trade orders', async () => {
    const accountId = uniqueId('acct-master');
    const session = await createExpertAdvisorSession(accountId, 'master-secret');
    await clearSessionOutbox(session);

    await approveExpertAdvisorSession({
      expertAdvisorId: accountId,
      sessionId: session.sessionId,
      accountId,
      authKeyFingerprint: session.authKeyFingerprint,
      approvedBy: 'ops-console',
    });

    await waitForSessionAuthentication(session);

    await enqueueExpertAdvisorTradeOrder({
      expertAdvisorId: accountId,
      sessionId: session.sessionId,
      accountId,
      command: {
        commandType: 'open',
        instrument: 'USDJPY',
        orderType: 'market',
        side: 'buy',
        volume: 1,
        stopLoss: 131.2,
        takeProfit: 132.75,
        timeInForce: 'gtc',
        clientOrderId: 'master-open-1',
        metadata: { source: 'ops-console' },
      },
    });

    await sleep(RETRY_DELAY_MS);
    const openEvents = await fetchSessionOutbox(session);
    const openOrder = findEvent(openEvents, 'OrderCommand');
    expect(openOrder).toBeTruthy();
    expect((openOrder?.payload as Record<string, unknown>).commandType).toBe('open');
    await acknowledgeOutboxEvents(session, openEvents);

    await enqueueExpertAdvisorTradeOrder({
      expertAdvisorId: accountId,
      sessionId: session.sessionId,
      accountId,
      command: {
        commandType: 'close',
        instrument: 'USDJPY',
        orderType: 'market',
        side: 'sell',
        volume: 1,
        timeInForce: 'ioc',
        positionId: 'pos-master-1',
        clientOrderId: 'master-close-1',
        metadata: { source: 'ops-console' },
      },
    });

    await sleep(RETRY_DELAY_MS);
    const closeEvents = await fetchSessionOutbox(session);
    const closeOrder = findEvent(closeEvents, 'OrderCommand');
    expect(closeOrder).toBeTruthy();
    const closePayload = closeOrder?.payload as Record<string, unknown> | undefined;
    expect(closePayload?.commandType).toBe('close');
    expect(closePayload?.positionId).toBe('pos-master-1');
    await acknowledgeOutboxEvents(session, closeEvents);
  });

  it('executes copy trading between a leader and follower', async () => {
    const leaderAccount = uniqueId('acct-leader');
    const followerAccount = uniqueId('acct-follower');
    const groupId = uniqueId('copy-group');

    const leader = await createExpertAdvisorSession(leaderAccount, 'leader-secret');
    const follower = await createExpertAdvisorSession(followerAccount, 'follower-secret');

    await Promise.all([clearSessionOutbox(leader), clearSessionOutbox(follower)]);

    await Promise.all([
      approveExpertAdvisorSession({
        expertAdvisorId: leaderAccount,
        sessionId: leader.sessionId,
        accountId: leaderAccount,
        authKeyFingerprint: leader.authKeyFingerprint,
        approvedBy: 'ops-console',
      }),
      approveExpertAdvisorSession({
        expertAdvisorId: followerAccount,
        sessionId: follower.sessionId,
        accountId: followerAccount,
        authKeyFingerprint: follower.authKeyFingerprint,
        approvedBy: 'ops-console',
      }),
    ]);

    await Promise.all([
      waitForSessionAuthentication(leader),
      waitForSessionAuthentication(follower),
    ]);

    await createCopyTradeGroup({
      groupId,
      name: `Group ${groupId}`,
      description: 'Copy trading cohort',
      requestedBy: 'ops-console',
    });

    await upsertCopyTradeGroupMember({
      groupId,
      memberId: leaderAccount,
      role: 'leader',
      riskStrategy: 'balanced',
      allocation: 1,
      requestedBy: 'ops-console',
    });
    await upsertCopyTradeGroupMember({
      groupId,
      memberId: followerAccount,
      role: 'follower',
      riskStrategy: 'balanced',
      allocation: 1,
      requestedBy: 'ops-console',
    });

    await sleep(RETRY_DELAY_MS);
    const followerUpdates = await fetchSessionOutbox(follower);
    const updateEvent = findEvent(followerUpdates, 'CopyTradeGroupUpdated');
    expect(updateEvent).toBeTruthy();
    expect((updateEvent?.payload as Record<string, unknown>).groupId).toBe(groupId);
    await acknowledgeOutboxEvents(follower, followerUpdates);

    await executeCopyTradeOrder({
      groupId,
      sourceAccount: leaderAccount,
      initiatedBy: 'ops-console',
      command: {
        commandType: 'open',
        instrument: 'EURUSD',
        orderType: 'market',
        side: 'buy',
        volume: 0.5,
        stopLoss: 1.0812,
        takeProfit: 1.0965,
        timeInForce: 'gtc',
        clientOrderId: 'copy-ord-1',
        metadata: { strategy: 'swing' },
      },
    });

    await sleep(RETRY_DELAY_MS);
    const followerOrders = await fetchSessionOutbox(follower);
    const copyOrder = findEvent(followerOrders, 'OrderCommand');
    expect(copyOrder).toBeTruthy();
    const copyPayload = copyOrder?.payload as Record<string, unknown> | undefined;
    expect(copyPayload?.metadata).toMatchObject({
      groupId,
      sourceAccount: leaderAccount,
    });
    await acknowledgeOutboxEvents(follower, followerOrders);

    const group = await getCopyTradeGroup(groupId);
    expect(group.members).toHaveLength(2);
  });

  it('routes independent copy trades for separate groups', async () => {
    const leaderA = await createExpertAdvisorSession(uniqueId('acct-alpha-leader'), 'alpha-secret');
    const followerA = await createExpertAdvisorSession(
      uniqueId('acct-alpha-follower'),
      'alpha-follow',
    );
    const leaderB = await createExpertAdvisorSession(uniqueId('acct-beta-leader'), 'beta-secret');
    const followerB = await createExpertAdvisorSession(
      uniqueId('acct-beta-follower'),
      'beta-follow',
    );

    const sessions: ExpertAdvisorSession[] = [leaderA, followerA, leaderB, followerB];
    await Promise.all(sessions.map(clearSessionOutbox));

    await Promise.all(
      sessions.map((session) =>
        approveExpertAdvisorSession({
          expertAdvisorId: session.accountId,
          sessionId: session.sessionId,
          accountId: session.accountId,
          authKeyFingerprint: session.authKeyFingerprint,
          approvedBy: 'ops-console',
        }),
      ),
    );

    await Promise.all(sessions.map((session) => waitForSessionAuthentication(session)));

    const groupA = uniqueId('momentum-alpha');
    const groupB = uniqueId('momentum-beta');

    await createCopyTradeGroup({
      groupId: groupA,
      name: 'Momentum Alpha',
      description: 'Alpha copy cohort',
      requestedBy: 'ops-console',
    });
    await createCopyTradeGroup({
      groupId: groupB,
      name: 'Momentum Beta',
      description: 'Beta copy cohort',
      requestedBy: 'ops-console',
    });

    await upsertCopyTradeGroupMember({
      groupId: groupA,
      memberId: leaderA.accountId,
      role: 'leader',
      riskStrategy: 'aggressive',
      allocation: 1,
      requestedBy: 'ops-console',
    });
    await upsertCopyTradeGroupMember({
      groupId: groupA,
      memberId: followerA.accountId,
      role: 'follower',
      riskStrategy: 'balanced',
      allocation: 1,
      requestedBy: 'ops-console',
    });
    await upsertCopyTradeGroupMember({
      groupId: groupB,
      memberId: leaderB.accountId,
      role: 'leader',
      riskStrategy: 'conservative',
      allocation: 1,
      requestedBy: 'ops-console',
    });
    await upsertCopyTradeGroupMember({
      groupId: groupB,
      memberId: followerB.accountId,
      role: 'follower',
      riskStrategy: 'balanced',
      allocation: 1,
      requestedBy: 'ops-console',
    });

    await sleep(RETRY_DELAY_MS);
    await Promise.all([
      acknowledgeOutboxEvents(followerA, await fetchSessionOutbox(followerA)),
      acknowledgeOutboxEvents(followerB, await fetchSessionOutbox(followerB)),
    ]);

    await executeCopyTradeOrder({
      groupId: groupA,
      sourceAccount: leaderA.accountId,
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
    await executeCopyTradeOrder({
      groupId: groupB,
      sourceAccount: leaderB.accountId,
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

    await sleep(RETRY_DELAY_MS);
    const followerAOrders = await fetchSessionOutbox(followerA);
    const followerBOrders = await fetchSessionOutbox(followerB);

    const orderA = findEvent(followerAOrders, 'OrderCommand');
    expect(orderA).toBeTruthy();
    expect((orderA?.payload as Record<string, unknown>).instrument).toBe('GBPUSD');
    expect((orderA?.payload as Record<string, any>).metadata.groupId).toBe(groupA);

    const orderB = findEvent(followerBOrders, 'OrderCommand');
    expect(orderB).toBeTruthy();
    expect((orderB?.payload as Record<string, unknown>).instrument).toBe('AUDUSD');
    expect((orderB?.payload as Record<string, any>).metadata.groupId).toBe(groupB);

    await acknowledgeOutboxEvents(followerA, followerAOrders);
    await acknowledgeOutboxEvents(followerB, followerBOrders);
  });

  it('delivers copy trades to shared followers across multiple groups', async () => {
    const leaderPrimary = await createExpertAdvisorSession(
      uniqueId('acct-swing-leader'),
      'swing-secret',
    );
    const leaderSecondary = await createExpertAdvisorSession(
      uniqueId('acct-hedge-leader'),
      'hedge-secret',
    );
    const followerShared = await createExpertAdvisorSession(
      uniqueId('acct-shared-follower'),
      'shared-secret',
    );

    const sessions: ExpertAdvisorSession[] = [leaderPrimary, leaderSecondary, followerShared];
    await Promise.all(sessions.map(clearSessionOutbox));

    await Promise.all(
      sessions.map((session) =>
        approveExpertAdvisorSession({
          expertAdvisorId: session.accountId,
          sessionId: session.sessionId,
          accountId: session.accountId,
          authKeyFingerprint: session.authKeyFingerprint,
          approvedBy: 'ops-console',
        }),
      ),
    );

    await Promise.all(sessions.map((session) => waitForSessionAuthentication(session)));

    const swingGroup = uniqueId('swing-cadre');
    const hedgeGroup = uniqueId('hedge-cadre');

    await createCopyTradeGroup({
      groupId: swingGroup,
      name: 'Swing Cadre',
      description: 'Swing cohort',
      requestedBy: 'ops-console',
    });
    await createCopyTradeGroup({
      groupId: hedgeGroup,
      name: 'Hedge Cadre',
      description: 'Hedge cohort',
      requestedBy: 'ops-console',
    });

    await upsertCopyTradeGroupMember({
      groupId: swingGroup,
      memberId: leaderPrimary.accountId,
      role: 'leader',
      riskStrategy: 'balanced',
      allocation: 1,
      requestedBy: 'ops-console',
    });
    await upsertCopyTradeGroupMember({
      groupId: swingGroup,
      memberId: followerShared.accountId,
      role: 'follower',
      riskStrategy: 'balanced',
      allocation: 1,
      requestedBy: 'ops-console',
    });
    await upsertCopyTradeGroupMember({
      groupId: hedgeGroup,
      memberId: leaderSecondary.accountId,
      role: 'leader',
      riskStrategy: 'conservative',
      allocation: 1,
      requestedBy: 'ops-console',
    });
    await upsertCopyTradeGroupMember({
      groupId: hedgeGroup,
      memberId: followerShared.accountId,
      role: 'follower',
      riskStrategy: 'balanced',
      allocation: 1,
      requestedBy: 'ops-console',
    });

    await sleep(RETRY_DELAY_MS);
    await acknowledgeOutboxEvents(followerShared, await fetchSessionOutbox(followerShared));

    await executeCopyTradeOrder({
      groupId: swingGroup,
      sourceAccount: leaderPrimary.accountId,
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
    await executeCopyTradeOrder({
      groupId: hedgeGroup,
      sourceAccount: leaderSecondary.accountId,
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

    await sleep(RETRY_DELAY_MS);
    const sharedOrders = await fetchSessionOutbox(followerShared);
    const swingOrder = sharedOrders.find(
      (event) =>
        event.eventType === 'OrderCommand' &&
        (event.payload as Record<string, any>).metadata.groupId === swingGroup,
    );
    const hedgeOrder = sharedOrders.find(
      (event) =>
        event.eventType === 'OrderCommand' &&
        (event.payload as Record<string, any>).metadata.groupId === hedgeGroup,
    );

    expect(swingOrder).toBeTruthy();
    expect((swingOrder?.payload as Record<string, unknown>).instrument).toBe('NZDJPY');
    expect(hedgeOrder).toBeTruthy();
    expect((hedgeOrder?.payload as Record<string, unknown>).instrument).toBe('USDCHF');

    await acknowledgeOutboxEvents(followerShared, sharedOrders);
  });
});

function uniqueId(prefix: string): string {
  return `${prefix}-${randomUUID().slice(0, 8)}`;
}

async function waitForGateway() {
  for (let attempt = 0; attempt < RETRY_ATTEMPTS; attempt += 1) {
    try {
      const response = await gatewayRequest('/trade-agent/v1/health');
      if (response.ok) {
        await response.arrayBuffer();
        return;
      }
    } catch {
      // retry
    }
    await sleep(RETRY_DELAY_MS);
  }
  throw new Error('Gateway service did not become ready in time.');
}

async function waitForManagement() {
  for (let attempt = 0; attempt < RETRY_ATTEMPTS; attempt += 1) {
    try {
      const response = await managementRequest('/admin/experts');
      if (response.ok) {
        await response.arrayBuffer();
        return;
      }
    } catch {
      // retry
    }
    await sleep(RETRY_DELAY_MS);
  }
  throw new Error('Management API did not become ready in time.');
}

async function waitForSessionAuthentication(session: ExpertAdvisorSession) {
  for (let attempt = 0; attempt < RETRY_ATTEMPTS; attempt += 1) {
    try {
      await fetchSessionOutbox(session);
      return;
    } catch {
      // retry if the session is not yet present
    }
    await sleep(RETRY_DELAY_MS);
  }
  throw new Error(`Session for ${session.accountId} did not authenticate in time.`);
}

function findEvent(events: Iterable<{ eventType: string }>, eventType: string) {
  for (const event of events) {
    if (event.eventType === eventType) {
      return event as { eventType: string; payload: Record<string, unknown> };
    }
  }
  return undefined;
}

async function sleep(durationMs: number) {
  await new Promise((resolve) => setTimeout(resolve, durationMs));
}
