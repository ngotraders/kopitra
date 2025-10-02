import { describe, expect, it } from 'vitest';
import { fetchDashboardTrends } from './fetchDashboardTrends';

describe('fetchDashboardTrends', () => {
  it('returns dashboard performance comparisons', async () => {
    const result = await fetchDashboardTrends();
    expect(result).toHaveLength(3);
    expect(result[1]).toMatchObject({ id: 'fills', delta: 11.2 });
  });
});
