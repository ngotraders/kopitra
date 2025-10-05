import { describe, expect, it, vi } from 'vitest';
import { fetchAdminUsers } from './fetchAdminUsers';

vi.mock('./opsConsoleSnapshot.ts', async () => await import('./__mocks__/opsConsoleSnapshot.ts'));

describe('fetchAdminUsers', () => {
  it('returns admin user records', async () => {
    const result = await fetchAdminUsers();
    expect(result[0]).toMatchObject({ role: 'Admin' });
  });
});
