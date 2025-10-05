import { describe, expect, it, vi } from 'vitest';
import { fetchOperationsPerformanceTrends } from './fetchOperationsPerformanceTrends';

vi.mock('./opsConsoleSnapshot.ts', async () => await import('./__mocks__/opsConsoleSnapshot.ts'));

describe('fetchOperationsPerformanceTrends', () => {
  it('returns performance funnel metrics for operations dashboards', async () => {
    const result = await fetchOperationsPerformanceTrends();
    expect(result).toEqual(expect.arrayContaining([expect.objectContaining({ id: 'conversion' })]));
  });
});
