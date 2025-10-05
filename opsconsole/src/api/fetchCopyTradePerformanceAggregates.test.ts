import { describe, expect, it, vi } from 'vitest';
import { fetchCopyTradePerformanceAggregates } from './fetchCopyTradePerformanceAggregates';

vi.mock('./opsConsoleSnapshot.ts', async () => await import('./__mocks__/opsConsoleSnapshot.ts'));

describe('fetchCopyTradePerformanceAggregates', () => {
  it('describes copy trade outcomes across environments', async () => {
    const aggregates = await fetchCopyTradePerformanceAggregates();
    expect(aggregates.length).toBeGreaterThan(0);
    const record = aggregates[0];
    expect(record).toMatchObject({
      timeframe: expect.any(String),
      environment: expect.any(String),
      notifications: expect.any(Number),
      fills: expect.any(Number),
      fillRate: expect.any(Number),
    });
  });
});
