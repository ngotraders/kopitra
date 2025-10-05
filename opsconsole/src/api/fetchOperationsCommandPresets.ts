import type { CommandPreset } from '../types/console.ts';
import { fetchOpsConsoleSnapshot } from './opsConsoleSnapshot.ts';

export async function fetchOperationsCommandPresets(): Promise<CommandPreset[]> {
  const snapshot = await fetchOpsConsoleSnapshot();
  return snapshot.commandPresets;
}
