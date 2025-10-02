import { describe, expect, it } from 'vitest';
import { fetchCurrentUser } from './fetchCurrentUser';

describe('fetchCurrentUser', () => {
  it('returns the authenticated console user', async () => {
    await expect(fetchCurrentUser()).resolves.toMatchObject({
      id: 'user-1',
      email: 'alex.morgan@example.com',
      roles: ['operator'],
    });
  });
});
