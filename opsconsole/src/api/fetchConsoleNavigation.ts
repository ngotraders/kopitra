import type { NavigationItem } from '../types/console.ts';
import { fetchOpsConsoleSnapshot } from './opsConsoleSnapshot.ts';

export async function fetchConsoleNavigation(): Promise<NavigationItem[]> {
  const snapshot = await fetchOpsConsoleSnapshot();
  return snapshot.navigationItems;
}
