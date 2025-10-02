import { describe, expect, it } from 'vitest';
import { fetchDashboardActivities } from './fetchDashboardActivities';

describe('fetchDashboardActivities', () => {
  it('provides the latest dashboard activities', async () => {
    const result = await fetchDashboardActivities();
    expect(result[0]).toMatchObject({
      id: 'act-1',
      status: 'success',
    });
  });
});
