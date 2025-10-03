import { describe, expect, it } from 'vitest';
import { fetchAdminUserDetail } from './fetchAdminUserDetail';

describe('fetchAdminUserDetail', () => {
  it('returns user details with activity history', async () => {
    const result = await fetchAdminUserDetail('user-1');
    expect(result.user).toHaveProperty('email');
    expect(result.activity.length).toBeGreaterThan(0);
  });

  it('throws when the user record is missing', async () => {
    await expect(fetchAdminUserDetail('missing-user')).rejects.toThrow(
      'User missing-user not found',
    );
  });
});
