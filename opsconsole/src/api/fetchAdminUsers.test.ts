import { describe, expect, it } from 'vitest';
import { fetchAdminUsers } from './fetchAdminUsers';

describe('fetchAdminUsers', () => {
  it('returns admin user records', async () => {
    const result = await fetchAdminUsers();
    expect(result[0]).toMatchObject({ role: 'Admin' });
  });
});
