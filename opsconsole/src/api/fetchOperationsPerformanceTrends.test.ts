import { describe, expect, it } from 'vitest';
import { fetchOperationsPerformanceTrends } from './fetchOperationsPerformanceTrends';

describe('fetchOperationsPerformanceTrends', () => {
  it('returns performance funnel metrics for operations dashboards', async () => {
    const result = await fetchOperationsPerformanceTrends();
    expect(result).toEqual(expect.arrayContaining([expect.objectContaining({ id: 'conversion' })]));
  });
});
