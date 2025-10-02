import { tradeAgentCommands, tradeAgentSessions, tradeAgents } from '../data/console.ts';
import type { TradeAgentDetail } from '../types/console.ts';
import { clone } from './utils.ts';

export async function fetchTradeAgentDetail(agentId: string): Promise<TradeAgentDetail> {
  const agent = tradeAgents.find((item) => item.id === agentId);

  if (!agent) {
    throw new Error(`Trade agent ${agentId} not found`);
  }

  return clone({
    agent,
    sessions: tradeAgentSessions[agent.id] ?? [],
    commands: tradeAgentCommands[agent.id] ?? [],
  });
}
