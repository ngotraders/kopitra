import {
  copyGroupMembers,
  copyGroupPerformance,
  copyGroupRoutes,
  copyGroupSummaries,
} from '../data/console.ts';
import type { CopyGroupDetail } from '../types/console.ts';
import { clone } from './utils.ts';

export async function fetchCopyGroupDetail(groupId: string): Promise<CopyGroupDetail> {
  const summary = copyGroupSummaries.find((group) => group.id === groupId);

  if (!summary) {
    throw new Error(`Copy group ${groupId} not found`);
  }

  return clone({
    group: summary,
    members: copyGroupMembers[groupId] ?? [],
    routes: copyGroupRoutes[groupId] ?? [],
    performance: copyGroupPerformance[groupId] ?? [],
  });
}
