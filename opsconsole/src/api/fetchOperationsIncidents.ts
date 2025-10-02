import { operationsIncidents } from '../data/console.ts';
import type { OperationsIncident } from '../types/console.ts';
import { clone } from './utils.ts';

export async function fetchOperationsIncidents(): Promise<OperationsIncident[]> {
  return clone(operationsIncidents);
}
