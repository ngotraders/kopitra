import type { AdminUserDetail } from '../types/console.ts';
import { fetchOpsConsoleSnapshot } from './opsConsoleSnapshot.ts';

export async function fetchAdminUserDetail(userId: string): Promise<AdminUserDetail> {
  const snapshot = await fetchOpsConsoleSnapshot();
  const record = snapshot.users.find((user) => user.id === userId);

  if (!record) {
    throw new Error(`User ${userId} not found`);
  }

  return {
    user: record,
    activity: snapshot.userActivity[userId] ?? [],
  };
}
