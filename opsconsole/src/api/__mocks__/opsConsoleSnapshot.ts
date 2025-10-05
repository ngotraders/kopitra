import { consoleSnapshot } from '../../data/console.ts';
import { clone } from '../utils.ts';

export async function fetchOpsConsoleSnapshot() {
  return clone(consoleSnapshot);
}

export function resetOpsConsoleSnapshotCache() {
  // no-op in mock
}
