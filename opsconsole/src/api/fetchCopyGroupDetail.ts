import type { CopyGroupDetail } from '../types/console.ts';
import { fetchOpsConsoleSnapshot } from './opsConsoleSnapshot.ts';

export async function fetchCopyGroupDetail(groupId: string): Promise<CopyGroupDetail> {
  const snapshot = await fetchOpsConsoleSnapshot();
  const summary = snapshot.copyGroupSummaries.find((group) => group.id === groupId);

  if (!summary) {
    throw new Error(`Copy group ${groupId} not found`);
  }

  return {
    group: summary,
    members: snapshot.copyGroupMembers[groupId] ?? [],
    routes: snapshot.copyGroupRoutes[groupId] ?? [],
    performance: snapshot.copyGroupPerformance[groupId] ?? [],
  };
}
