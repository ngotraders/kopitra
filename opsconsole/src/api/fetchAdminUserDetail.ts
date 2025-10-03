import { userActivity, users } from '../data/console.ts';
import type { AdminUserDetail } from '../types/console.ts';
import { clone } from './utils.ts';

export async function fetchAdminUserDetail(userId: string): Promise<AdminUserDetail> {
  const record = users.find((user) => user.id === userId);

  if (!record) {
    throw new Error(`User ${userId} not found`);
  }

  return clone({
    user: record,
    activity: userActivity[userId] ?? [],
  });
}
