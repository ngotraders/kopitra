import { expect, test } from '@playwright/test';
import type { Page } from '@playwright/test';

interface OutboxEvent {
  id: string;
  eventType: string;
  payload: Record<string, any>;
  requiresAck: boolean;
}

async function navigateToWorkbench(page: Page) {
  await test.step('navigate to integration workbench', async () => {
    await page.goto('/integration/copy-trading');
    await expect(page.getByRole('heading', { name: /copy trading integration/i })).toBeVisible();
  });
}

async function connectExpertAdvisor(page: Page, accountId: string, authKey: string) {
  await test.step(`connect expert advisor ${accountId}`, async () => {
    await page.getByTestId('connect-account').fill(accountId);
    await page.getByTestId('connect-auth').fill(authKey);
    await page.getByTestId('connect-submit').click();
    await expect(page.getByTestId(`session-row-${accountId}`)).toBeVisible();
  });
}

async function approveSession(page: Page, accountId: string, approvedBy: string) {
  await test.step(`approve session ${accountId}`, async () => {
    await page.getByTestId('approve-actor').fill(approvedBy);
    await page.getByTestId(`approve-session-${accountId}`).click();
    await expect(page.getByTestId(`session-approved-${accountId}`)).toHaveText('Yes');
  });
}

async function clearOutbox(page: Page, accountId: string) {
  await test.step(`clear outbox ${accountId}`, async () => {
    await page.getByTestId(`clear-outbox-${accountId}`).click();
    await expect(page.getByTestId(`session-status-${accountId}`)).toContainText('Outbox cleared');
  });
}

async function refreshOutbox(page: Page, accountId: string): Promise<OutboxEvent[]> {
  await page.getByTestId(`fetch-outbox-${accountId}`).click();
  await expect(page.getByTestId(`session-status-${accountId}`)).toContainText('Fetched');
  const serialized = await page.getByTestId(`outbox-json-${accountId}`).inputValue();
  return JSON.parse(serialized) as OutboxEvent[];
}

async function acknowledgeOutbox(page: Page, accountId: string) {
  await page.getByTestId(`ack-outbox-${accountId}`).click();
  await expect(page.getByTestId(`session-status-${accountId}`)).toContainText(
    'Events acknowledged',
  );
}

async function sendDirectOrder(
  page: Page,
  params: {
    sessionAccountId: string;
    commandType: string;
    instrument: string;
    side: string;
    volume: string;
    timeInForce?: string;
    orderType?: string;
    clientOrderId?: string;
    stopLoss?: string;
    takeProfit?: string;
    positionId?: string;
  },
) {
  await test.step(`send ${params.commandType} order for ${params.sessionAccountId}`, async () => {
    await page.getByTestId('trade-session').selectOption(params.sessionAccountId);
    await page.getByTestId('trade-command-type').selectOption(params.commandType);
    await page.getByTestId('trade-instrument').fill(params.instrument);
    if (params.orderType) {
      await page.getByTestId('trade-order-type').selectOption(params.orderType);
    }
    await page.getByTestId('trade-side').selectOption(params.side);
    await page.getByTestId('trade-volume').fill(params.volume);
    if (params.timeInForce) {
      await page.getByTestId('trade-tif').fill(params.timeInForce);
    }
    if (params.clientOrderId) {
      await page.getByTestId('trade-client-order').fill(params.clientOrderId);
    }
    if (params.stopLoss) {
      await page.getByTestId('trade-stop-loss').fill(params.stopLoss);
    }
    if (params.takeProfit) {
      await page.getByTestId('trade-take-profit').fill(params.takeProfit);
    }
    if (params.positionId) {
      await page.getByTestId('trade-position-id').fill(params.positionId);
    }
    await page.getByTestId('trade-submit').click();
    await expect(page.getByTestId('workbench-status')).toContainText('Trade order sent');
  });
}

async function createCopyGroup(
  page: Page,
  groupId: string,
  name: string,
  requestedBy = 'ops-console',
) {
  await test.step(`create copy group ${groupId}`, async () => {
    await page.getByTestId('group-id').fill(groupId);
    await page.getByTestId('group-name').fill(name);
    await page.getByTestId('group-requested-by').fill(requestedBy);
    await page.getByTestId('group-submit').click();
    await expect(page.getByTestId('workbench-status')).toContainText(
      `Copy group ${groupId} created`,
    );
    await expect(page.getByTestId(`group-card-${groupId}`)).toBeVisible();
  });
}

async function addGroupMember(
  page: Page,
  params: {
    groupId: string;
    accountId: string;
    role: string;
    riskStrategy: string;
    allocation: string;
    requestedBy?: string;
  },
) {
  await test.step(`add member ${params.accountId} to ${params.groupId}`, async () => {
    await page.getByTestId('member-group').selectOption(params.groupId);
    await page.getByTestId('member-account').selectOption(params.accountId);
    await page.getByTestId('member-role').selectOption(params.role);
    await page.getByTestId('member-risk').fill(params.riskStrategy);
    await page.getByTestId('member-allocation').fill(params.allocation);
    if (params.requestedBy) {
      await page.getByTestId('member-requested-by').fill(params.requestedBy);
    }
    await page.getByTestId('member-submit').click();
    await expect(page.getByTestId('workbench-status')).toContainText(`Added ${params.accountId}`);
    await expect(page.getByTestId(`group-card-${params.groupId}`)).toContainText(params.accountId);
  });
}

async function executeCopyTrade(
  page: Page,
  params: {
    groupId: string;
    sourceAccount: string;
    initiatedBy: string;
    commandType: string;
    instrument: string;
    side: string;
    volume: string;
    clientOrderId?: string;
    timeInForce?: string;
    stopLoss?: string;
    takeProfit?: string;
    positionId?: string;
  },
) {
  await test.step(`execute copy trade for ${params.groupId}`, async () => {
    await page.getByTestId('copy-group').selectOption(params.groupId);
    await page.getByTestId('copy-source').selectOption(params.sourceAccount);
    await page.getByTestId('copy-initiated-by').fill(params.initiatedBy);
    await page.getByTestId('copy-command-type').selectOption(params.commandType);
    await page.getByTestId('copy-instrument').fill(params.instrument);
    await page.getByTestId('copy-side').selectOption(params.side);
    await page.getByTestId('copy-volume').fill(params.volume);
    if (params.clientOrderId) {
      await page.getByTestId('copy-client-order').fill(params.clientOrderId);
    }
    if (params.timeInForce) {
      await page.getByTestId('copy-tif').fill(params.timeInForce);
    }
    if (params.stopLoss) {
      await page.getByTestId('copy-stop-loss').fill(params.stopLoss);
    }
    if (params.takeProfit) {
      await page.getByTestId('copy-take-profit').fill(params.takeProfit);
    }
    if (params.positionId) {
      await page.getByTestId('copy-position-id').fill(params.positionId);
    }
    await page.getByTestId('copy-submit').click();
    await expect(page.getByTestId('workbench-status')).toContainText(
      `Copy trade dispatched for ${params.groupId}`,
    );
  });
}

test.describe.serial('copy trading management flows via ops console', () => {
  test.beforeEach(async ({ page }) => {
    await navigateToWorkbench(page);
  });

  test('scenario 1: single EA order lifecycle', async ({ page }) => {
    await connectExpertAdvisor(page, 'acct-master', 'master-secret');
    await clearOutbox(page, 'acct-master');
    await approveSession(page, 'acct-master', 'ops-console');

    await sendDirectOrder(page, {
      sessionAccountId: 'acct-master',
      commandType: 'open',
      instrument: 'USDJPY',
      side: 'buy',
      volume: '1',
      timeInForce: 'gtc',
      orderType: 'market',
      clientOrderId: 'master-open-1',
      stopLoss: '131.2',
      takeProfit: '132.75',
    });

    const openEvents = await refreshOutbox(page, 'acct-master');
    const openOrder = openEvents.find((event) => event.eventType === 'OrderCommand');
    expect(openOrder?.payload['commandType']).toBe('open');
    expect(openOrder?.payload['instrument']).toBe('USDJPY');
    await acknowledgeOutbox(page, 'acct-master');

    await sendDirectOrder(page, {
      sessionAccountId: 'acct-master',
      commandType: 'close',
      instrument: 'USDJPY',
      side: 'sell',
      volume: '1',
      timeInForce: 'ioc',
      orderType: 'market',
      clientOrderId: 'master-close-1',
      positionId: 'pos-master-1',
    });

    const closeEvents = await refreshOutbox(page, 'acct-master');
    const closeOrder = closeEvents.find((event) => event.eventType === 'OrderCommand');
    expect(closeOrder?.payload['commandType']).toBe('close');
    expect(closeOrder?.payload['positionId']).toBe('pos-master-1');
    await acknowledgeOutbox(page, 'acct-master');
  });

  test('scenario 2: dual EA copy trading within one group', async ({ page }) => {
    await connectExpertAdvisor(page, 'acct-leader', 'leader-secret');
    await connectExpertAdvisor(page, 'acct-follower', 'follower-secret');

    await clearOutbox(page, 'acct-leader');
    await clearOutbox(page, 'acct-follower');

    await approveSession(page, 'acct-leader', 'ops-console');
    await approveSession(page, 'acct-follower', 'ops-console');

    await createCopyGroup(page, 'swing-alpha', 'Swing Alpha');
    await addGroupMember(page, {
      groupId: 'swing-alpha',
      accountId: 'acct-leader',
      role: 'leader',
      riskStrategy: 'balanced',
      allocation: '1',
    });
    await addGroupMember(page, {
      groupId: 'swing-alpha',
      accountId: 'acct-follower',
      role: 'follower',
      riskStrategy: 'balanced',
      allocation: '1',
    });

    const membershipEvents = await refreshOutbox(page, 'acct-follower');
    expect(
      membershipEvents.some((event) => event.eventType === 'CopyTradeGroupUpdated'),
    ).toBeTruthy();
    await acknowledgeOutbox(page, 'acct-follower');

    await executeCopyTrade(page, {
      groupId: 'swing-alpha',
      sourceAccount: 'acct-leader',
      initiatedBy: 'ops-console',
      commandType: 'open',
      instrument: 'EURUSD',
      side: 'buy',
      volume: '0.5',
      clientOrderId: 'copy-order-1',
      timeInForce: 'gtc',
      stopLoss: '1.0812',
      takeProfit: '1.0975',
    });

    const followerOrders = await refreshOutbox(page, 'acct-follower');
    const copyOrder = followerOrders.find((event) => event.eventType === 'OrderCommand');
    expect(copyOrder?.payload['instrument']).toBe('EURUSD');
    expect(copyOrder?.payload['metadata']?.['groupId']).toBe('swing-alpha');
    await acknowledgeOutbox(page, 'acct-follower');
  });

  test('scenario 3: independent orders across multiple groups', async ({ page }) => {
    await connectExpertAdvisor(page, 'acct-alpha-leader', 'alpha-secret');
    await connectExpertAdvisor(page, 'acct-alpha-follower', 'alpha-follow');
    await connectExpertAdvisor(page, 'acct-beta-leader', 'beta-secret');
    await connectExpertAdvisor(page, 'acct-beta-follower', 'beta-follow');

    for (const account of [
      'acct-alpha-leader',
      'acct-alpha-follower',
      'acct-beta-leader',
      'acct-beta-follower',
    ]) {
      await clearOutbox(page, account);
      await approveSession(page, account, 'ops-console');
    }

    await createCopyGroup(page, 'momentum-alpha', 'Momentum Alpha');
    await createCopyGroup(page, 'momentum-beta', 'Momentum Beta');

    await addGroupMember(page, {
      groupId: 'momentum-alpha',
      accountId: 'acct-alpha-leader',
      role: 'leader',
      riskStrategy: 'aggressive',
      allocation: '1',
    });
    await addGroupMember(page, {
      groupId: 'momentum-alpha',
      accountId: 'acct-alpha-follower',
      role: 'follower',
      riskStrategy: 'balanced',
      allocation: '1',
    });
    await addGroupMember(page, {
      groupId: 'momentum-beta',
      accountId: 'acct-beta-leader',
      role: 'leader',
      riskStrategy: 'conservative',
      allocation: '1',
    });
    await addGroupMember(page, {
      groupId: 'momentum-beta',
      accountId: 'acct-beta-follower',
      role: 'follower',
      riskStrategy: 'balanced',
      allocation: '1',
    });

    for (const account of ['acct-alpha-follower', 'acct-beta-follower']) {
      const updates = await refreshOutbox(page, account);
      expect(updates.some((event) => event.eventType === 'CopyTradeGroupUpdated')).toBeTruthy();
      await acknowledgeOutbox(page, account);
    }

    await executeCopyTrade(page, {
      groupId: 'momentum-alpha',
      sourceAccount: 'acct-alpha-leader',
      initiatedBy: 'ops-alpha',
      commandType: 'open',
      instrument: 'GBPUSD',
      side: 'buy',
      volume: '0.8',
      clientOrderId: 'alpha-ord-1',
      timeInForce: 'gtc',
      stopLoss: '1.2512',
      takeProfit: '1.2695',
    });

    await executeCopyTrade(page, {
      groupId: 'momentum-beta',
      sourceAccount: 'acct-beta-leader',
      initiatedBy: 'ops-beta',
      commandType: 'open',
      instrument: 'AUDUSD',
      side: 'sell',
      volume: '0.4',
      clientOrderId: 'beta-ord-1',
      timeInForce: 'gtc',
      stopLoss: '0.6652',
      takeProfit: '0.6511',
    });

    const alphaOrders = await refreshOutbox(page, 'acct-alpha-follower');
    const alphaOrder = alphaOrders.find((event) => event.eventType === 'OrderCommand');
    expect(alphaOrder?.payload['instrument']).toBe('GBPUSD');
    expect(alphaOrder?.payload['metadata']?.['groupId']).toBe('momentum-alpha');
    await acknowledgeOutbox(page, 'acct-alpha-follower');

    const betaOrders = await refreshOutbox(page, 'acct-beta-follower');
    const betaOrder = betaOrders.find((event) => event.eventType === 'OrderCommand');
    expect(betaOrder?.payload['instrument']).toBe('AUDUSD');
    expect(betaOrder?.payload['metadata']?.['groupId']).toBe('momentum-beta');
    await acknowledgeOutbox(page, 'acct-beta-follower');
  });

  test('scenario 4: overlapping memberships execute independent orders', async ({ page }) => {
    await connectExpertAdvisor(page, 'acct-cross-leader', 'cross-leader-secret');
    await connectExpertAdvisor(page, 'acct-cross-follower', 'cross-follower-secret');

    await clearOutbox(page, 'acct-cross-leader');
    await clearOutbox(page, 'acct-cross-follower');

    await approveSession(page, 'acct-cross-leader', 'ops-console');
    await approveSession(page, 'acct-cross-follower', 'ops-console');

    await createCopyGroup(page, 'cross-alpha', 'Cross Alpha');
    await createCopyGroup(page, 'cross-beta', 'Cross Beta');

    await addGroupMember(page, {
      groupId: 'cross-alpha',
      accountId: 'acct-cross-leader',
      role: 'leader',
      riskStrategy: 'balanced',
      allocation: '1',
    });
    await addGroupMember(page, {
      groupId: 'cross-alpha',
      accountId: 'acct-cross-follower',
      role: 'follower',
      riskStrategy: 'balanced',
      allocation: '1',
    });
    await addGroupMember(page, {
      groupId: 'cross-beta',
      accountId: 'acct-cross-leader',
      role: 'leader',
      riskStrategy: 'balanced',
      allocation: '1',
    });
    await addGroupMember(page, {
      groupId: 'cross-beta',
      accountId: 'acct-cross-follower',
      role: 'follower',
      riskStrategy: 'balanced',
      allocation: '1',
    });

    const followerUpdates = await refreshOutbox(page, 'acct-cross-follower');
    expect(
      followerUpdates.filter((event) => event.eventType === 'CopyTradeGroupUpdated'),
    ).toHaveLength(2);
    await acknowledgeOutbox(page, 'acct-cross-follower');

    await executeCopyTrade(page, {
      groupId: 'cross-alpha',
      sourceAccount: 'acct-cross-leader',
      initiatedBy: 'ops-alpha',
      commandType: 'open',
      instrument: 'NZDUSD',
      side: 'buy',
      volume: '0.6',
      clientOrderId: 'cross-alpha-1',
      timeInForce: 'gtc',
      stopLoss: '0.6012',
      takeProfit: '0.6195',
    });

    await executeCopyTrade(page, {
      groupId: 'cross-beta',
      sourceAccount: 'acct-cross-leader',
      initiatedBy: 'ops-beta',
      commandType: 'open',
      instrument: 'USDCHF',
      side: 'sell',
      volume: '0.7',
      clientOrderId: 'cross-beta-1',
      timeInForce: 'gtc',
      stopLoss: '0.9122',
      takeProfit: '0.8985',
    });

    const crossOrders = await refreshOutbox(page, 'acct-cross-follower');
    const instruments = crossOrders
      .filter((event) => event.eventType === 'OrderCommand')
      .map((event) => event.payload['instrument']);
    expect(instruments).toEqual(expect.arrayContaining(['NZDUSD', 'USDCHF']));
    await acknowledgeOutbox(page, 'acct-cross-follower');
  });
});
