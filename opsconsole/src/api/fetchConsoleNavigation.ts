import { navigationItems } from '../data/console.ts';
import type { NavigationItem } from '../types/console.ts';
import { clone } from './utils.ts';

export async function fetchConsoleNavigation(): Promise<NavigationItem[]> {
  return clone(navigationItems);
}
