import { describe, expect, it, vi } from 'vitest';
import { fetchOperationsHealth } from './fetchOperationsHealth';

vi.mock('./opsConsoleSnapshot.ts', async () => await import('./__mocks__/opsConsoleSnapshot.ts'));

describe('fetchOperationsHealth', () => {
  it('returns high-level operational KPIs', async () => {
    const result = await fetchOperationsHealth();
    expect(result).toEqual(
      expect.arrayContaining([expect.objectContaining({ id: 'agents-online' })]),
    );
  });
});
