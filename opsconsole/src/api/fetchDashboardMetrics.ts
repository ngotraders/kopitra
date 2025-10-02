import { statMetrics } from '../data/console.ts';
import type { StatMetric } from '../types/console.ts';
import { clone } from './utils.ts';

export async function fetchDashboardMetrics(): Promise<StatMetric[]> {
  return clone(statMetrics);
}
