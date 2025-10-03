import { useMemo } from 'react';

export interface ExpertAdvisorSession {
  accountId: string;
  sessionId: string;
  sessionToken: string;
  authKeyFingerprint: string;
}

export interface OutboxEvent {
  id: string;
  eventType: string;
  payload: Record<string, unknown>;
  requiresAck: boolean;
}

export interface TradeCommandInput {
  accountId: string;
  sessionId: string;
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

export interface CopyTradeExecutionInput {
  groupId: string;
  sourceAccount: string;
  initiatedBy?: string;
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

export interface CopyGroupMemberInput {
  groupId: string;
  accountId: string;
  role: string;
  riskStrategy: string;
  allocation: number;
  requestedBy: string;
}

export interface CreateCopyGroupInput {
  groupId: string;
  name: string;
  description?: string;
  requestedBy: string;
}

export interface CopyTradingClient {
  connectExpertAdvisor(accountId: string, authenticationKey: string): Promise<ExpertAdvisorSession>;
  approveExpertAdvisorSession(
    session: ExpertAdvisorSession,
    approvedBy: string,
  ): Promise<void>;
  clearOutbox(session: ExpertAdvisorSession): Promise<void>;
  fetchOutbox(session: ExpertAdvisorSession): Promise<OutboxEvent[]>;
  acknowledgeOutbox(session: ExpertAdvisorSession, events: OutboxEvent[]): Promise<void>;
  enqueueTradeOrder(input: TradeCommandInput): Promise<void>;
  createCopyGroup(input: CreateCopyGroupInput): Promise<void>;
  upsertCopyGroupMember(input: CopyGroupMemberInput): Promise<void>;
  executeCopyTrade(input: CopyTradeExecutionInput): Promise<void>;
}

interface HttpErrorPayload {
  error?: string;
  message?: string;
}

function createIdempotencyKey(): string {
  if (typeof crypto !== 'undefined' && 'randomUUID' in crypto) {
    return crypto.randomUUID();
  }
  return Math.random().toString(36).slice(2);
}

async function ensureOk(response: Response): Promise<void> {
  if (response.ok) {
    return;
  }

  let errorMessage = `${response.status} ${response.statusText}`;
  try {
    const payload = (await response.json()) as HttpErrorPayload;
    if (payload?.error || payload?.message) {
      errorMessage = payload.error ?? payload.message ?? errorMessage;
    }
  } catch (error) {
    console.warn('Failed to parse error payload', error);
  }
  throw new Error(errorMessage);
}

function resolveBaseUrl(envKey: string, fallback: string): string {
  const value = (import.meta as { env: Record<string, string | undefined> }).env[envKey];
  return value && value.length ? value : fallback;
}

async function computeAuthFingerprint(accountId: string, authenticationKey: string): Promise<string> {
  const encoder = new TextEncoder();
  const preimage = `account_session_key:${accountId}:${authenticationKey}`;
  const digest = await crypto.subtle.digest('SHA-256', encoder.encode(preimage));
  const bytes = Array.from(new Uint8Array(digest));
  return bytes.map((byte) => byte.toString(16).padStart(2, '0')).join('');
}

function toCommandBody(input: TradeCommandInput): Record<string, unknown> {
  const body: Record<string, unknown> = {
    accountId: input.accountId,
    commandType: input.commandType,
    instrument: input.instrument,
  };

  if (input.orderType) body.orderType = input.orderType;
  if (input.side) body.side = input.side;
  if (input.volume !== undefined) body.volume = input.volume;
  if (input.price !== undefined) body.price = input.price;
  if (input.stopLoss !== undefined) body.stopLoss = input.stopLoss;
  if (input.takeProfit !== undefined) body.takeProfit = input.takeProfit;
  if (input.timeInForce) body.timeInForce = input.timeInForce;
  if (input.positionId) body.positionId = input.positionId;
  if (input.clientOrderId) body.clientOrderId = input.clientOrderId;
  if (input.metadata) body.metadata = input.metadata;

  return body;
}

function toExecutionBody(input: CopyTradeExecutionInput): Record<string, unknown> {
  const body: Record<string, unknown> = {
    sourceAccount: input.sourceAccount,
    commandType: input.commandType,
    instrument: input.instrument,
  };

  if (input.orderType) body.orderType = input.orderType;
  if (input.side) body.side = input.side;
  if (input.volume !== undefined) body.volume = input.volume;
  if (input.price !== undefined) body.price = input.price;
  if (input.stopLoss !== undefined) body.stopLoss = input.stopLoss;
  if (input.takeProfit !== undefined) body.takeProfit = input.takeProfit;
  if (input.timeInForce) body.timeInForce = input.timeInForce;
  if (input.positionId) body.positionId = input.positionId;
  if (input.clientOrderId) body.clientOrderId = input.clientOrderId;
  if (input.initiatedBy) body.initiatedBy = input.initiatedBy;
  if (input.metadata) body.metadata = input.metadata;

  return body;
}

export function createCopyTradingClient(): CopyTradingClient {
  const gatewayBaseUrl = resolveBaseUrl('VITE_GATEWAY_BASE_URL', 'http://localhost:8080');
  const managementBaseUrl = resolveBaseUrl('VITE_MANAGEMENT_BASE_URL', 'http://localhost:7071/api');
  const opsBearerToken = resolveBaseUrl('VITE_OPS_BEARER_TOKEN', 'dev-token');

  async function gatewayRequest(path: string, init: RequestInit = {}): Promise<Response> {
    return fetch(`${gatewayBaseUrl}${path}`, {
      ...init,
      headers: {
        Accept: 'application/json',
        ...(init.headers ?? {}),
      },
    });
  }

  async function managementRequest(path: string, init: RequestInit = {}): Promise<Response> {
    return fetch(`${managementBaseUrl}${path}`, {
      ...init,
      headers: {
        Accept: 'application/json',
        Authorization: `Bearer ${opsBearerToken}`,
        'X-TradeAgent-Account': 'console',
        ...(init.headers ?? {}),
      },
    });
  }

  return {
    async connectExpertAdvisor(accountId, authenticationKey) {
      const response = await gatewayRequest('/trade-agent/v1/sessions', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-TradeAgent-Account': accountId,
          'Idempotency-Key': createIdempotencyKey(),
        },
        body: JSON.stringify({
          authMethod: 'account_session_key',
          authenticationKey,
        }),
      });
      await ensureOk(response);
      const payload = (await response.json()) as { sessionId: string; sessionToken: string };
      return {
        accountId,
        sessionId: payload.sessionId,
        sessionToken: payload.sessionToken,
        authKeyFingerprint: await computeAuthFingerprint(accountId, authenticationKey),
      };
    },

    async approveExpertAdvisorSession(session, approvedBy) {
      const response = await managementRequest(
        `/admin/experts/${encodeURIComponent(session.accountId)}/sessions/${encodeURIComponent(session.sessionId)}/approve`,
        {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify({
            accountId: session.accountId,
            authKeyFingerprint: session.authKeyFingerprint,
            approvedBy,
          }),
        },
      );
      await ensureOk(response);
    },

    async clearOutbox(session) {
      const events = await this.fetchOutbox(session);
      if (!events.length) {
        return;
      }
      await this.acknowledgeOutbox(session, events);
    },

    async fetchOutbox(session) {
      const response = await gatewayRequest('/trade-agent/v1/sessions/current/outbox', {
        headers: {
          'X-TradeAgent-Account': session.accountId,
          Authorization: `Bearer ${session.sessionToken}`,
        },
      });
      await ensureOk(response);
      const payload = (await response.json()) as { events: OutboxEvent[] };
      return payload.events ?? [];
    },

    async acknowledgeOutbox(session, events) {
      for (const event of events) {
        if (!event.requiresAck) {
          continue;
        }
        const response = await gatewayRequest(
          `/trade-agent/v1/sessions/current/outbox/${encodeURIComponent(event.id)}/ack`,
          {
            method: 'POST',
            headers: {
              'X-TradeAgent-Account': session.accountId,
              Authorization: `Bearer ${session.sessionToken}`,
              'Idempotency-Key': createIdempotencyKey(),
            },
          },
        );
        await ensureOk(response);
      }
    },

    async enqueueTradeOrder(input) {
      const response = await managementRequest(
        `/admin/experts/${encodeURIComponent(input.accountId)}/sessions/${encodeURIComponent(input.sessionId)}/trade-orders`,
        {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify(toCommandBody(input)),
        },
      );
      await ensureOk(response);
    },

    async createCopyGroup(input) {
      const response = await managementRequest('/admin/copy-trade/groups', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          groupId: input.groupId,
          name: input.name,
          description: input.description ?? `Group ${input.name}`,
          requestedBy: input.requestedBy,
        }),
      });
      await ensureOk(response);
    },

    async upsertCopyGroupMember(input) {
      const response = await managementRequest(
        `/admin/copy-trade/groups/${encodeURIComponent(input.groupId)}/members/${encodeURIComponent(input.accountId)}`,
        {
          method: 'PUT',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify({
            role: input.role,
            riskStrategy: input.riskStrategy,
            allocation: input.allocation,
            requestedBy: input.requestedBy,
          }),
        },
      );
      await ensureOk(response);
    },

    async executeCopyTrade(input) {
      const response = await managementRequest(
        `/admin/copy-trade/groups/${encodeURIComponent(input.groupId)}/orders`,
        {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify(toExecutionBody(input)),
        },
      );
      await ensureOk(response);
    },
  } satisfies CopyTradingClient;
}

export function useCopyTradingClient(): CopyTradingClient {
  return useMemo(() => createCopyTradingClient(), []);
}

