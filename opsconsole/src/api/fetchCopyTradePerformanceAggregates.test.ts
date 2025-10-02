import { describe, expect, it } from 'vitest';
import { fetchCopyTradePerformanceAggregates } from './fetchCopyTradePerformanceAggregates';

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
