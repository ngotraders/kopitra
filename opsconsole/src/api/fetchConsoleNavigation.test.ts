import { describe, expect, it } from 'vitest';
import { fetchConsoleNavigation } from './fetchConsoleNavigation';

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
