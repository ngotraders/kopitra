import { tradeAgents } from '../data/console.ts';
import type { TradeAgentSummary } from '../types/console.ts';
import { clone } from './utils.ts';

export async function fetchTradeAgents(): Promise<TradeAgentSummary[]> {
  return clone(tradeAgents);
}
