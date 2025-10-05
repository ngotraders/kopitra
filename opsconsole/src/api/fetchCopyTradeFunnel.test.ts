import { describe, expect, it, vi } from 'vitest';
import { fetchCopyTradeFunnel } from './fetchCopyTradeFunnel';

vi.mock('./opsConsoleSnapshot.ts', async () => await import('./__mocks__/opsConsoleSnapshot.ts'));

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
