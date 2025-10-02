import { afterEach, describe, expect, it, vi } from 'vitest';
import { resetTelemetrySubscribers, subscribeToTelemetry, trackTelemetry } from './telemetry';

describe('telemetry', () => {
  afterEach(() => {
    resetTelemetrySubscribers();
  });

  it('dispatches events to subscribers', () => {
    const handler = vi.fn();
    const unsubscribe = subscribeToTelemetry(handler);

    trackTelemetry({
      type: 'command.issued',
      commandId: 'cmd-123',
      scope: 'Copy group APAC Momentum',
      operator: 'Alex Morgan',
    });

    expect(handler).toHaveBeenCalledWith({
      type: 'command.issued',
      commandId: 'cmd-123',
      scope: 'Copy group APAC Momentum',
      operator: 'Alex Morgan',
    });

    unsubscribe();
    trackTelemetry({
      type: 'command.failed',
      commandId: 'cmd-456',
      scope: 'Trade agent TA-1402',
      operator: 'Alex Morgan',
      reason: 'Timeout',
    });

    expect(handler).toHaveBeenCalledTimes(1);
  });
});
