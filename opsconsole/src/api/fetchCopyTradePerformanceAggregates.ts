import type { CopyTradePerformanceAggregate } from '../types/console.ts';
import { fetchOpsConsoleSnapshot } from './opsConsoleSnapshot.ts';

export async function fetchCopyTradePerformanceAggregates(): Promise<
  CopyTradePerformanceAggregate[]
> {
  const snapshot = await fetchOpsConsoleSnapshot();
  return snapshot.copyTradePerformanceAggregates;
}
