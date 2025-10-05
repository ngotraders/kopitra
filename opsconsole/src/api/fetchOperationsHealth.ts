import type { HealthKpi } from '../types/console.ts';
import { fetchOpsConsoleSnapshot } from './opsConsoleSnapshot.ts';

export async function fetchOperationsHealth(): Promise<HealthKpi[]> {
  const snapshot = await fetchOpsConsoleSnapshot();
  return snapshot.operationsHealth;
}
