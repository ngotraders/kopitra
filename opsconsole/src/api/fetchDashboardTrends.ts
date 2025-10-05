import type { PerformanceTrend } from '../types/console.ts';
import { fetchOpsConsoleSnapshot } from './opsConsoleSnapshot.ts';

export async function fetchDashboardTrends(): Promise<PerformanceTrend[]> {
  const snapshot = await fetchOpsConsoleSnapshot();
  return snapshot.dashboardTrends;
}
