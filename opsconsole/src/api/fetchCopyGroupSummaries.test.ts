import { describe, expect, it, vi } from 'vitest';
import { fetchCopyGroupSummaries } from './fetchCopyGroupSummaries';

vi.mock('./opsConsoleSnapshot.ts', async () => await import('./__mocks__/opsConsoleSnapshot.ts'));

describe('fetchCopyGroupSummaries', () => {
  it('returns copy group catalogue summaries', async () => {
    const result = await fetchCopyGroupSummaries();
    expect(result).toHaveLength(3);
    expect(result[0]).toMatchObject({ name: 'APAC Momentum' });
  });
});
