import { describe, expect, it } from 'vitest';
import { fetchCopyGroupSummaries } from './fetchCopyGroupSummaries';

describe('fetchCopyGroupSummaries', () => {
  it('returns copy group catalogue summaries', async () => {
    const result = await fetchCopyGroupSummaries();
    expect(result).toHaveLength(3);
    expect(result[0]).toMatchObject({ name: 'APAC Momentum' });
  });
});
