import { describe, expect, it, vi } from 'vitest';
import { fetchTradeAgents } from './fetchTradeAgents';

vi.mock('./opsConsoleSnapshot.ts', async () => await import('./__mocks__/opsConsoleSnapshot.ts'));

describe('fetchTradeAgents', () => {
  it('returns the trade agent catalogue', async () => {
    const result = await fetchTradeAgents();
    expect(result[0]).toHaveProperty('environment', 'Production');
  });
});
