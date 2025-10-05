import { describe, expect, it, vi } from 'vitest';
import { fetchCopyGroupDetail } from './fetchCopyGroupDetail';

vi.mock('./opsConsoleSnapshot.ts', async () => await import('./__mocks__/opsConsoleSnapshot.ts'));

describe('fetchCopyGroupDetail', () => {
  it('returns a composite payload for the requested copy group', async () => {
    const result = await fetchCopyGroupDetail('asia-momentum');
    expect(result.group).toMatchObject({ name: 'APAC Momentum' });
    expect(result.members).not.toHaveLength(0);
    expect(result.routes[0]).toHaveProperty('destination');
  });

  it('throws when the copy group does not exist', async () => {
    await expect(fetchCopyGroupDetail('missing')).rejects.toThrow('Copy group missing not found');
  });
});
