export type TelemetryEvent =
  | {
      type: 'command.issued';
      commandId: string;
      scope: string;
      operator: string;
    }
  | {
      type: 'command.failed';
      commandId: string;
      scope: string;
      operator: string;
      reason: string;
    };

export type TelemetrySubscriber = (event: TelemetryEvent) => void;

const subscribers = new Set<TelemetrySubscriber>();

export function trackTelemetry(event: TelemetryEvent) {
  subscribers.forEach((subscriber) => subscriber(event));
}

export function subscribeToTelemetry(subscriber: TelemetrySubscriber) {
  subscribers.add(subscriber);
  return () => subscribers.delete(subscriber);
}

export function resetTelemetrySubscribers() {
  subscribers.clear();
}
