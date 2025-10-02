import { commandEvents } from '../data/console.ts';
import type { CommandEvent } from '../types/console.ts';
import { clone } from './utils.ts';

export async function fetchOperationsCommandEvents(): Promise<CommandEvent[]> {
  return clone(commandEvents);
}
