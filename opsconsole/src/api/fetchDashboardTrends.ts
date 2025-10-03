import { dashboardTrends } from '../data/console.ts';
import type { PerformanceTrend } from '../types/console.ts';
import { clone } from './utils.ts';

export async function fetchDashboardTrends(): Promise<PerformanceTrend[]> {
  return clone(dashboardTrends);
}
