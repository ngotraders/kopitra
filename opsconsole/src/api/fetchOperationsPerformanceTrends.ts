import { operationsPerformanceTrends } from '../data/console.ts';
import type { PerformanceTrend } from '../types/console.ts';
import { clone } from './utils.ts';

export async function fetchOperationsPerformanceTrends(): Promise<PerformanceTrend[]> {
  return clone(operationsPerformanceTrends);
}
