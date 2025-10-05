import { describe, expect, it, vi } from 'vitest';
import { fetchDashboardTrends } from './fetchDashboardTrends';

vi.mock('./opsConsoleSnapshot.ts', async () => await import('./__mocks__/opsConsoleSnapshot.ts'));

describe('fetchDashboardTrends', () => {
  it('returns dashboard performance comparisons', async () => {
    const result = await fetchDashboardTrends();
    expect(result).toHaveLength(3);
    expect(result[1]).toMatchObject({ id: 'fills', delta: 11.2 });
  });
});
