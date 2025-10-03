import { describe, expect, it } from 'vitest';
import { fetchCopyTradeFunnel } from './fetchCopyTradeFunnel';

describe('fetchCopyTradeFunnel', () => {
  it('provides funnel stages with conversion data', async () => {
    const stages = await fetchCopyTradeFunnel();
    expect(stages.length).toBeGreaterThan(0);
    expect(stages[0]).toMatchObject({
      label: expect.any(String),
      notifications: expect.any(Number),
      fills: expect.any(Number),
    });
  });
});
