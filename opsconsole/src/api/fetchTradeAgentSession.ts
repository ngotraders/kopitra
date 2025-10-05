import type { TradeAgentSessionDetail } from '../types/console.ts';
import { fetchOpsConsoleSnapshot } from './opsConsoleSnapshot.ts';

export async function fetchTradeAgentSession(
  agentId: string,
  sessionId: string,
): Promise<TradeAgentSessionDetail> {
  const snapshot = await fetchOpsConsoleSnapshot();
  const agent = snapshot.tradeAgents.find((item) => item.id === agentId);

  if (!agent) {
    throw new Error(`Trade agent ${agentId} not found`);
  }

  const sessions = snapshot.tradeAgentSessions[agent.id] ?? [];
  const session = sessions.find((item) => item.id === sessionId);

  if (!session) {
    throw new Error(`Session ${sessionId} not found for trade agent ${agentId}`);
  }

  return {
    agent,
    session,
    logs: snapshot.tradeAgentLogs[session.id] ?? [],
  };
}
