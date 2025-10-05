import { describe, expect, it, vi } from 'vitest';
import { fetchCurrentUser } from './fetchCurrentUser';

vi.mock('./opsConsoleSnapshot.ts', async () => await import('./__mocks__/opsConsoleSnapshot.ts'));

describe('fetchCurrentUser', () => {
  it('returns the authenticated console user', async () => {
    await expect(fetchCurrentUser()).resolves.toMatchObject({
      id: 'user-1',
      email: 'alex.morgan@example.com',
      roles: ['operator'],
    });
  });
});
