import { commandPresets } from '../data/console.ts';
import type { CommandPreset } from '../types/console.ts';
import { clone } from './utils.ts';

export async function fetchOperationsCommandPresets(): Promise<CommandPreset[]> {
  return clone(commandPresets);
}
