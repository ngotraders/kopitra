import { copyGroupSummaries } from '../data/console.ts';
import type { CopyGroupSummary } from '../types/console.ts';
import { clone } from './utils.ts';

export async function fetchCopyGroupSummaries(): Promise<CopyGroupSummary[]> {
  return clone(copyGroupSummaries);
}
