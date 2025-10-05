import type { TradeAgentDetail } from '../types/console.ts';
import { fetchOpsConsoleSnapshot } from './opsConsoleSnapshot.ts';

export async function fetchTradeAgentDetail(agentId: string): Promise<TradeAgentDetail> {
  const snapshot = await fetchOpsConsoleSnapshot();
  const agent = snapshot.tradeAgents.find((item) => item.id === agentId);

  if (!agent) {
    throw new Error(`Trade agent ${agentId} not found`);
  }

  return {
    agent,
    sessions: snapshot.tradeAgentSessions[agent.id] ?? [],
    commands: snapshot.tradeAgentCommands[agent.id] ?? [],
  };
}
