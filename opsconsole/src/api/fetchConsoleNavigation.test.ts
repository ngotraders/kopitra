import { describe, expect, it, vi } from 'vitest';
import { fetchConsoleNavigation } from './fetchConsoleNavigation';

vi.mock('./opsConsoleSnapshot.ts', async () => await import('./__mocks__/opsConsoleSnapshot.ts'));

describe('fetchConsoleNavigation', () => {
  it('returns navigation items for the console sidebar', async () => {
    const result = await fetchConsoleNavigation();
    expect(result).toHaveLength(6);
    expect(result[0]).toEqual(
      expect.objectContaining({ id: 'dashboard', label: 'Dashboard', to: '/dashboard/activity' }),
    );
    expect(result[5]).toEqual(
      expect.objectContaining({
        id: 'integration',
        label: 'Integration',
        to: '/integration/copy-trading',
      }),
    );
  });
});
