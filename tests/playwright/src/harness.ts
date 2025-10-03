import { APIRequestContext, request, expect } from '@playwright/test';
import crypto from 'node:crypto';

export interface TradeCommand {
  commandType: string;
  instrument: string;
  orderType?: string;
  side?: string;
  volume?: number;
  price?: number;
  stopLoss?: number;
  takeProfit?: number;
  timeInForce?: string;
  positionId?: string;
  clientOrderId?: string;
  metadata?: Record<string, unknown>;
}

export interface CopyTradeExecution {
  sourceAccount: string;
  initiatedBy?: string;
  command: TradeCommand;
}

export interface OutboundEvent {
  id: string;
  eventType: string;
  payload: Record<string, unknown>;
  requiresAck: boolean;
}

export interface EaSession {
  account: string;
  authFingerprint: string;
  sessionId: string;
  sessionToken: string;
}

export class CopyTradingHarness {
  private constructor(
    private readonly gateway: APIRequestContext,
    private readonly management: APIRequestContext,
  ) {}

  static async create(): Promise<CopyTradingHarness> {
    const gatewayBase = process.env.GATEWAY_BASE_URL ?? 'http://localhost:8080';
    const managementBase = process.env.MANAGEMENT_BASE_URL ?? 'http://localhost:7071/api';
    const opsToken = process.env.OPS_BEARER_TOKEN ?? 'dev-token';

    const gateway = await request.newContext({
      baseURL: gatewayBase,
      extraHTTPHeaders: {
        Accept: 'application/json',
      },
      timeout: 15_000,
    });

    const management = await request.newContext({
      baseURL: managementBase,
      extraHTTPHeaders: {
        Accept: 'application/json',
        Authorization: `Bearer ${opsToken}`,
        'X-TradeAgent-Account': 'console',
      },
      timeout: 15_000,
    });

    return new CopyTradingHarness(gateway, management);
  }

  async dispose(): Promise<void> {
    await Promise.all([this.gateway.dispose(), this.management.dispose()]);
  }

  async connectEa(account: string, authKey: string): Promise<EaSession> {
    const response = await this.gateway.post('/trade-agent/v1/sessions', {
      headers: {
        'X-TradeAgent-Account': account,
        'Idempotency-Key': crypto.randomUUID(),
      },
      data: {
        authMethod: 'account_session_key',
        authenticationKey: authKey,
      },
    });

    expect(response.ok()).toBeTruthy();
    const created = await response.json();

    return {
      account,
      authFingerprint: this.hashSecret(authKey, account),
      sessionId: created.sessionId,
      sessionToken: created.sessionToken,
    };
  }

  async approveSession(session: EaSession, approvedBy?: string): Promise<void> {
    const response = await this.management.post(
      `/admin/experts/${session.account}/sessions/${session.sessionId}/approve`,
      {
        data: {
          accountId: session.account,
          authKeyFingerprint: session.authFingerprint,
          approvedBy,
        },
      },
    );

    expect(response.ok()).toBeTruthy();
  }

  async assertSessionAuthenticated(session: EaSession): Promise<void> {
    const response = await this.gateway.get('/trade-agent/v1/sessions/current/outbox', {
      headers: {
        'X-TradeAgent-Account': session.account,
        Authorization: `Bearer ${session.sessionToken}`,
      },
    });

    expect(response.ok()).toBeTruthy();
  }

  async enqueueTradeOrder(session: EaSession, command: TradeCommand): Promise<void> {
    const response = await this.management.post(
      `/admin/experts/${session.account}/sessions/${session.sessionId}/trade-orders`,
      {
        data: this.toCommandBody(session.account, command),
      },
    );

    expect(response.ok()).toBeTruthy();
  }

  async createCopyGroup(groupId: string, name: string, requestedBy: string): Promise<void> {
    const response = await this.management.post('/admin/copy-trade/groups', {
      data: {
        groupId,
        name,
        description: `Group ${name}`,
        requestedBy,
      },
    });

    expect(response.ok()).toBeTruthy();
  }

  async upsertGroupMember(
    groupId: string,
    session: EaSession,
    role: string,
    riskStrategy: string,
    allocation: number,
    requestedBy: string,
  ): Promise<void> {
    const response = await this.management.put(
      `/admin/copy-trade/groups/${groupId}/members/${session.account}`,
      {
        data: {
          role,
          riskStrategy,
          allocation,
          requestedBy,
        },
      },
    );

    expect(response.ok()).toBeTruthy();
  }

  async executeCopyTrade(groupId: string, execution: CopyTradeExecution): Promise<void> {
    const response = await this.management.post(`/admin/copy-trade/groups/${groupId}/orders`, {
      data: this.toExecutionBody(execution),
    });

    expect(response.ok()).toBeTruthy();
  }

  async fetchOutbox(session: EaSession): Promise<OutboundEvent[]> {
    const response = await this.gateway.get('/trade-agent/v1/sessions/current/outbox', {
      headers: {
        'X-TradeAgent-Account': session.account,
        Authorization: `Bearer ${session.sessionToken}`,
      },
    });

    expect(response.ok()).toBeTruthy();
    const payload = await response.json();
    return payload.events ?? [];
  }

  async pollOutboxUntil(
    session: EaSession,
    predicate: (events: OutboundEvent[]) => boolean,
    attempts = 10,
    intervalMs = 250,
  ): Promise<OutboundEvent[]> {
    for (let attempt = 0; attempt < attempts; attempt += 1) {
      const events = await this.fetchOutbox(session);
      if (events.length && predicate(events)) {
        return events;
      }
      if (events.length) {
        await this.ackEvents(session, events);
      }
      await this.delay(intervalMs);
    }

    throw new Error('timed out waiting for matching outbox events');
  }

  async clearOutbox(session: EaSession): Promise<void> {
    const events = await this.fetchOutbox(session);
    if (events.length) {
      await this.ackEvents(session, events);
    }
  }

  async ackEvents(session: EaSession, events: OutboundEvent[]): Promise<void> {
    for (const event of events) {
      if (!event.requiresAck) {
        continue;
      }

      const response = await this.gateway.post(
        `/trade-agent/v1/sessions/current/outbox/${event.id}/ack`,
        {
          headers: {
            'X-TradeAgent-Account': session.account,
            Authorization: `Bearer ${session.sessionToken}`,
            'Idempotency-Key': crypto.randomUUID(),
          },
        },
      );

      expect(response.ok()).toBeTruthy();
    }
  }

  private toCommandBody(account: string, command: TradeCommand): Record<string, unknown> {
    const body: Record<string, unknown> = {
      accountId: account,
      commandType: command.commandType,
      instrument: command.instrument,
    };

    if (command.orderType) body.orderType = command.orderType;
    if (command.side) body.side = command.side;
    if (command.volume !== undefined) body.volume = command.volume;
    if (command.price !== undefined) body.price = command.price;
    if (command.stopLoss !== undefined) body.stopLoss = command.stopLoss;
    if (command.takeProfit !== undefined) body.takeProfit = command.takeProfit;
    if (command.timeInForce) body.timeInForce = command.timeInForce;
    if (command.positionId) body.positionId = command.positionId;
    if (command.clientOrderId) body.clientOrderId = command.clientOrderId;
    if (command.metadata) body.metadata = command.metadata;

    return body;
  }

  private toExecutionBody(execution: CopyTradeExecution): Record<string, unknown> {
    const body: Record<string, unknown> = {
      sourceAccount: execution.sourceAccount,
      commandType: execution.command.commandType,
      instrument: execution.command.instrument,
    };

    if (execution.command.orderType) body.orderType = execution.command.orderType;
    if (execution.command.side) body.side = execution.command.side;
    if (execution.command.volume !== undefined) body.volume = execution.command.volume;
    if (execution.command.price !== undefined) body.price = execution.command.price;
    if (execution.command.stopLoss !== undefined) body.stopLoss = execution.command.stopLoss;
    if (execution.command.takeProfit !== undefined) body.takeProfit = execution.command.takeProfit;
    if (execution.command.timeInForce) body.timeInForce = execution.command.timeInForce;
    if (execution.command.positionId) body.positionId = execution.command.positionId;
    if (execution.command.clientOrderId) body.clientOrderId = execution.command.clientOrderId;
    if (execution.initiatedBy) body.initiatedBy = execution.initiatedBy;
    if (execution.command.metadata) body.metadata = execution.command.metadata;

    return body;
  }

  private hashSecret(secret: string, account: string): string {
    const hash = crypto.createHash('sha256');
    hash.update('account_session_key');
    hash.update(':');
    hash.update(account);
    hash.update(':');
    hash.update(secret);
    return hash.digest('hex');
  }

  private async delay(ms: number): Promise<void> {
    await new Promise((resolve) => setTimeout(resolve, ms));
  }
}
