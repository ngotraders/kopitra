import type { OperationsIncident } from '../types/console.ts';
import { fetchOpsConsoleSnapshot } from './opsConsoleSnapshot.ts';

export async function fetchOperationsIncidents(): Promise<OperationsIncident[]> {
  const snapshot = await fetchOpsConsoleSnapshot();
  return snapshot.operationsIncidents;
}
