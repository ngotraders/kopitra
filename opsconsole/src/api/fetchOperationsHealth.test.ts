import { describe, expect, it } from 'vitest';
import { fetchOperationsHealth } from './fetchOperationsHealth';

describe('fetchOperationsHealth', () => {
  it('returns high-level operational KPIs', async () => {
    const result = await fetchOperationsHealth();
    expect(result).toEqual(
      expect.arrayContaining([expect.objectContaining({ id: 'agents-online' })]),
    );
  });
});
