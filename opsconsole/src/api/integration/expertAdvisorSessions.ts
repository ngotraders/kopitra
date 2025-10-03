import { createHash, randomUUID } from 'node:crypto';
import { expectGatewayOk, gatewayRequest } from './config.ts';

export interface ExpertAdvisorSession {
  accountId: string;
  sessionId: string;
  sessionToken: string;
  authKeyFingerprint: string;
}

export interface SessionSummary {
  accountId: string;
  sessionId: string;
  status: 'pending' | 'authenticated' | 'terminated';
  authMethod: 'account_session_key' | 'pre_shared_key';
  authKeyFingerprint: string;
  createdAt: string;
  updatedAt: string;
  lastHeartbeatAt?: string | null;
}

export interface OutboxEvent {
  id: string;
  sequence: number;
  eventType: string;
  payload: Record<string, unknown>;
  requiresAck: boolean;
}

interface SessionCreateResponse {
  sessionId: string;
  sessionToken: string;
}

interface OutboxFetchResponse {
  events: OutboxEvent[];
}

export async function createExpertAdvisorSession(
  accountId: string,
  authenticationKey: string,
): Promise<ExpertAdvisorSession> {
  const response = await gatewayRequest('/trade-agent/v1/sessions', {
    method: 'POST',
    headers: {
      'X-TradeAgent-Account': accountId,
      'Idempotency-Key': randomUUID(),
    },
    body: JSON.stringify({
      authMethod: 'account_session_key',
      authenticationKey,
    }),
  });
  await expectGatewayOk(response);
  const payload = (await response.json()) as SessionCreateResponse;
  return {
    accountId,
    sessionId: payload.sessionId,
    sessionToken: payload.sessionToken,
    authKeyFingerprint: computeAuthKeyFingerprint(authenticationKey, accountId),
  };
}

export async function getActiveSessionSummary(accountId: string): Promise<SessionSummary> {
  const response = await gatewayRequest(
    `/trade-agent/v1/admin/accounts/${encodeURIComponent(accountId)}/sessions/active`,
  );
  await expectGatewayOk(response);
  return (await response.json()) as SessionSummary;
}

export async function fetchSessionOutbox(session: ExpertAdvisorSession): Promise<OutboxEvent[]> {
  const response = await gatewayRequest('/trade-agent/v1/sessions/current/outbox', {
    headers: {
      'X-TradeAgent-Account': session.accountId,
      Authorization: `Bearer ${session.sessionToken}`,
    },
  });
  await expectGatewayOk(response);
  const payload = (await response.json()) as OutboxFetchResponse;
  return payload.events;
}

export async function acknowledgeOutboxEvents(
  session: ExpertAdvisorSession,
  events: Iterable<OutboxEvent>,
): Promise<void> {
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
          'Idempotency-Key': randomUUID(),
        },
      },
    );
    await expectGatewayOk(response);
  }
}

export async function clearSessionOutbox(session: ExpertAdvisorSession): Promise<void> {
  const events = await fetchSessionOutbox(session);
  if (events.length === 0) {
    return;
  }
  await acknowledgeOutboxEvents(session, events);
}

export function computeAuthKeyFingerprint(authenticationKey: string, accountId: string): string {
  const hash = createHash('sha256');
  hash.update('account_session_key');
  hash.update(':');
  hash.update(accountId);
  hash.update(':');
  hash.update(authenticationKey);
  return hash.digest('hex');
}
