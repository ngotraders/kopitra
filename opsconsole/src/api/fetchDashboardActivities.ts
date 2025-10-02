import { activities } from '../data/console.ts';
import type { Activity } from '../types/console.ts';
import { clone } from './utils.ts';

export async function fetchDashboardActivities(): Promise<Activity[]> {
  return clone(activities);
}
