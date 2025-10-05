import type { TradeAgentSummary } from '../types/console.ts';
import { fetchOpsConsoleSnapshot } from './opsConsoleSnapshot.ts';

export async function fetchTradeAgents(): Promise<TradeAgentSummary[]> {
  const snapshot = await fetchOpsConsoleSnapshot();
  return snapshot.tradeAgents;
}
