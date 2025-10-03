import { users } from '../data/console.ts';
import type { UserRecord } from '../types/console.ts';
import { clone } from './utils.ts';

export async function fetchAdminUsers(): Promise<UserRecord[]> {
  return clone(users);
}
