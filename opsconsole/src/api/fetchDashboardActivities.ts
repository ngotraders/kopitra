import type { Activity } from '../types/console.ts';
import { fetchOpsConsoleSnapshot } from './opsConsoleSnapshot.ts';

export async function fetchDashboardActivities(): Promise<Activity[]> {
  const snapshot = await fetchOpsConsoleSnapshot();
  return snapshot.activities;
}
