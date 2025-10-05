import { describe, expect, it, vi } from 'vitest';
import { fetchOperationsCommandPresets } from './fetchOperationsCommandPresets';

vi.mock('./opsConsoleSnapshot.ts', async () => await import('./__mocks__/opsConsoleSnapshot.ts'));

describe('fetchOperationsCommandPresets', () => {
  it('returns command presets used in the operations workspace', async () => {
    const result = await fetchOperationsCommandPresets();
    expect(result).toHaveLength(3);
  });
});
