import type { CopyGroupSummary } from '../types/console.ts';
import { fetchOpsConsoleSnapshot } from './opsConsoleSnapshot.ts';

export async function fetchCopyGroupSummaries(): Promise<CopyGroupSummary[]> {
  const snapshot = await fetchOpsConsoleSnapshot();
  return snapshot.copyGroupSummaries;
}
