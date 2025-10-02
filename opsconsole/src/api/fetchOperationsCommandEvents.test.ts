import { describe, expect, it } from 'vitest';
import { fetchOperationsCommandEvents } from './fetchOperationsCommandEvents';

describe('fetchOperationsCommandEvents', () => {
  it('returns command events for operations views', async () => {
    const result = await fetchOperationsCommandEvents();
    expect(result[0]).toMatchObject({ command: 'Pause replication' });
  });
});
