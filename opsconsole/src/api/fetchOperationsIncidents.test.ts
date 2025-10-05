import { describe, expect, it, vi } from 'vitest';
import { fetchOperationsIncidents } from './fetchOperationsIncidents';

vi.mock('./opsConsoleSnapshot.ts', async () => await import('./__mocks__/opsConsoleSnapshot.ts'));

describe('fetchOperationsIncidents', () => {
  it('returns immutable incident data', async () => {
    const incidents = await fetchOperationsIncidents();
    expect(incidents.length).toBeGreaterThan(0);
    const first = incidents[0];
    expect(first).toMatchObject({
      id: expect.stringContaining('incident-'),
      title: expect.any(String),
      severity: expect.any(String),
    });

    if (incidents.length > 0) {
      expect(incidents).not.toBe(await fetchOperationsIncidents());
    }
  });
});
