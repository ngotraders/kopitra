import type { PerformanceTrend } from '../types/console.ts';
import { fetchOpsConsoleSnapshot } from './opsConsoleSnapshot.ts';

export async function fetchOperationsPerformanceTrends(): Promise<PerformanceTrend[]> {
  const snapshot = await fetchOpsConsoleSnapshot();
  return snapshot.operationsPerformanceTrends;
}
