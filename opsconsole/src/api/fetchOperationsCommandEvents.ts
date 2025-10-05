import type { CommandEvent } from '../types/console.ts';
import { fetchOpsConsoleSnapshot } from './opsConsoleSnapshot.ts';

export async function fetchOperationsCommandEvents(): Promise<CommandEvent[]> {
  const snapshot = await fetchOpsConsoleSnapshot();
  return snapshot.commandEvents;
}
