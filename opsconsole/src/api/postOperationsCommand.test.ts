import { afterEach, describe, expect, it, vi } from 'vitest';
import { postOperationsCommand } from './postOperationsCommand';

const mocks = vi.hoisted(() => {
  const managementRequestMock = vi.fn(async () =>
    new Response(
      JSON.stringify({
        id: 'cmd-123',
        command: 'Restart agent',
        scope: 'Trade agent TA-1402',
        operator: 'Casey Rivers',
        issuedAt: '2024-04-22T08:12:00Z',
        status: 'pending',
      }),
    ),
  );
  const expectManagementOkMock = vi.fn(async () => undefined);
  return { managementRequestMock, expectManagementOkMock };
});

vi.mock('./integration/config.ts', () => ({
  managementRequest: mocks.managementRequestMock,
  expectManagementOk: mocks.expectManagementOkMock,
}));

describe('postOperationsCommand', () => {
  afterEach(() => {
    vi.clearAllMocks();
  });

  it('issues a pending command event with the provided scope', async () => {
    const event = await postOperationsCommand({
      command: 'Restart agent',
      scope: 'Trade agent TA-1402',
      operator: 'Casey Rivers',
    });

    expect(mocks.managementRequestMock).toHaveBeenCalledWith(
      '/opsconsole/commands',
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({
          command: 'Restart agent',
          scope: 'Trade agent TA-1402',
          operator: 'Casey Rivers',
        }),
      }),
    );
    expect(mocks.expectManagementOkMock).toHaveBeenCalled();
    expect(event).toMatchObject({
      command: 'Restart agent',
      scope: 'Trade agent TA-1402',
      operator: 'Casey Rivers',
      status: 'pending',
    });
    expect(new Date(event.issuedAt).toString()).not.toBe('Invalid Date');
  });
});
