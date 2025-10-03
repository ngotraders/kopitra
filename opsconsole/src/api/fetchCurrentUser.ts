import { currentUser } from '../data/console.ts';
import type { ConsoleUser } from '../types/console.ts';
import { clone } from './utils.ts';

export async function fetchCurrentUser(): Promise<ConsoleUser> {
  return clone(currentUser);
}
