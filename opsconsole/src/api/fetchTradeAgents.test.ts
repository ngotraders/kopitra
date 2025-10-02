import { describe, expect, it } from 'vitest';
import { fetchTradeAgents } from './fetchTradeAgents';

describe('fetchTradeAgents', () => {
  it('returns the trade agent catalogue', async () => {
    const result = await fetchTradeAgents();
    expect(result[0]).toHaveProperty('environment', 'Production');
  });
});
