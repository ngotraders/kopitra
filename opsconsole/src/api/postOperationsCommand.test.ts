import { afterEach, describe, expect, it, vi } from 'vitest';
import { postOperationsCommand } from './postOperationsCommand';

const originalDateNow = Date.now;

describe('postOperationsCommand', () => {
  afterEach(() => {
    Date.now = originalDateNow;
    vi.restoreAllMocks();
  });

  it('issues a pending command event with the provided scope', async () => {
    const dateSpy = vi.spyOn(Date, 'now').mockReturnValue(1713777600000);
    vi.spyOn(globalThis.Math, 'random').mockReturnValue(0.42);

    const event = await postOperationsCommand({
      command: 'Restart agent',
      scope: 'Trade agent TA-1402',
      operator: 'Casey Rivers',
    });

    expect(event).toMatchObject({
      command: 'Restart agent',
      scope: 'Trade agent TA-1402',
      operator: 'Casey Rivers',
      status: 'pending',
    });
    expect(new Date(event.issuedAt).toString()).not.toBe('Invalid Date');
    expect(event.id).toMatch(/^cmd-/);

    dateSpy.mockRestore();
  });
});
