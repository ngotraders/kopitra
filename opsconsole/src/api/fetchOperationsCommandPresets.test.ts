import { describe, expect, it } from 'vitest';
import { fetchOperationsCommandPresets } from './fetchOperationsCommandPresets';

describe('fetchOperationsCommandPresets', () => {
  it('returns command presets used in the operations workspace', async () => {
    const result = await fetchOperationsCommandPresets();
    expect(result).toHaveLength(3);
  });
});
