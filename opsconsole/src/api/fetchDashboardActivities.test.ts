import { describe, expect, it, vi } from 'vitest';
import { fetchDashboardActivities } from './fetchDashboardActivities';

vi.mock('./opsConsoleSnapshot.ts', async () => await import('./__mocks__/opsConsoleSnapshot.ts'));

describe('fetchDashboardActivities', () => {
  it('provides the latest dashboard activities', async () => {
    const result = await fetchDashboardActivities();
    expect(result[0]).toMatchObject({
      id: 'act-1',
      status: 'success',
    });
  });
});
