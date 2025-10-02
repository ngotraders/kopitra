import { tradeAgentLogs, tradeAgentSessions, tradeAgents } from '../data/console.ts';
import type { TradeAgentSessionDetail } from '../types/console.ts';
import { clone } from './utils.ts';

export async function fetchTradeAgentSession(
  agentId: string,
  sessionId: string,
): Promise<TradeAgentSessionDetail> {
  const agent = tradeAgents.find((item) => item.id === agentId);

  if (!agent) {
    throw new Error(`Trade agent ${agentId} not found`);
  }

  const sessions = tradeAgentSessions[agent.id] ?? [];
  const session = sessions.find((item) => item.id === sessionId);

  if (!session) {
    throw new Error(`Session ${sessionId} not found for trade agent ${agentId}`);
  }

  return clone({
    agent,
    session,
    logs: tradeAgentLogs[session.id] ?? [],
  });
}
