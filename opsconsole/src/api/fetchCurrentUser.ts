import type { ConsoleUser } from '../types/console.ts';
import { fetchOpsConsoleSnapshot } from './opsConsoleSnapshot.ts';

export async function fetchCurrentUser(): Promise<ConsoleUser> {
  const snapshot = await fetchOpsConsoleSnapshot();
  return snapshot.currentUser;
}
