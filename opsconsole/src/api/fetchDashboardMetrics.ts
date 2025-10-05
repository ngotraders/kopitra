import type { StatMetric } from '../types/console.ts';
import { fetchOpsConsoleSnapshot } from './opsConsoleSnapshot.ts';

export async function fetchDashboardMetrics(): Promise<StatMetric[]> {
  const snapshot = await fetchOpsConsoleSnapshot();
  return snapshot.statMetrics;
}
