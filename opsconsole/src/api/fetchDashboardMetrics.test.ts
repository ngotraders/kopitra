import { describe, expect, it } from 'vitest';
import { fetchDashboardMetrics } from './fetchDashboardMetrics';

describe('fetchDashboardMetrics', () => {
  it('returns stat metrics used across dashboard views', async () => {
    const result = await fetchDashboardMetrics();
    expect(result).toEqual(expect.arrayContaining([expect.objectContaining({ id: 'copy-rate' })]));
  });
});
