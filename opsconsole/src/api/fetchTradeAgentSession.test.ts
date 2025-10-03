import { describe, expect, it } from 'vitest';
import { fetchTradeAgentSession } from './fetchTradeAgentSession';

describe('fetchTradeAgentSession', () => {
  it('returns session details and logs', async () => {
    const result = await fetchTradeAgentSession('ta-1402', 'session-9001');
    expect(result.session).toHaveProperty('brokerAccount');
    expect(result.logs.length).toBeGreaterThan(0);
  });

  it('throws when the session is missing', async () => {
    await expect(fetchTradeAgentSession('ta-1402', 'missing')).rejects.toThrow(
      'Session missing not found for trade agent ta-1402',
    );
  });
});
