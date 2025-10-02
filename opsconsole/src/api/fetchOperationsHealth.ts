import { operationsHealth } from '../data/console.ts';
import type { HealthKpi } from '../types/console.ts';
import { clone } from './utils.ts';

export async function fetchOperationsHealth(): Promise<HealthKpi[]> {
  return clone(operationsHealth);
}
