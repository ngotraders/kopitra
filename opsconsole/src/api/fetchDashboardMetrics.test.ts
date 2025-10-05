import { describe, expect, it, vi } from 'vitest';
import { fetchDashboardMetrics } from './fetchDashboardMetrics';

vi.mock('./opsConsoleSnapshot.ts', async () => await import('./__mocks__/opsConsoleSnapshot.ts'));

describe('fetchDashboardMetrics', () => {
  it('returns stat metrics used across dashboard views', async () => {
    const result = await fetchDashboardMetrics();
    expect(result).toEqual(expect.arrayContaining([expect.objectContaining({ id: 'copy-rate' })]));
  });
});
