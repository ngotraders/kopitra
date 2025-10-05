import { describe, expect, it, vi } from 'vitest';
import { fetchTradeAgentDetail } from './fetchTradeAgentDetail';

vi.mock('./opsConsoleSnapshot.ts', async () => await import('./__mocks__/opsConsoleSnapshot.ts'));

describe('fetchTradeAgentDetail', () => {
  it('returns detail payload for a trade agent', async () => {
    const result = await fetchTradeAgentDetail('ta-1402');
    expect(result.agent).toMatchObject({ name: 'EA Stellar Momentum' });
    expect(result.sessions).not.toHaveLength(0);
  });

  it('throws when the agent cannot be located', async () => {
    await expect(fetchTradeAgentDetail('missing-agent')).rejects.toThrow(
      'Trade agent missing-agent not found',
    );
  });
});
