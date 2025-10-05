import type { UserRecord } from '../types/console.ts';
import { fetchOpsConsoleSnapshot } from './opsConsoleSnapshot.ts';

export async function fetchAdminUsers(): Promise<UserRecord[]> {
  const snapshot = await fetchOpsConsoleSnapshot();
  return snapshot.users;
}
