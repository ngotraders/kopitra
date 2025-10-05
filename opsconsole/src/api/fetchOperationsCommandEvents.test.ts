import { describe, expect, it, vi } from 'vitest';
import { fetchOperationsCommandEvents } from './fetchOperationsCommandEvents';

vi.mock('./opsConsoleSnapshot.ts', async () => await import('./__mocks__/opsConsoleSnapshot.ts'));

describe('fetchOperationsCommandEvents', () => {
  it('returns command events for operations views', async () => {
    const result = await fetchOperationsCommandEvents();
    expect(result[0]).toMatchObject({ command: 'Pause replication' });
  });
});
