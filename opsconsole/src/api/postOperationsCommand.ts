import type { CommandEvent } from '../types/console.ts';

export interface PostOperationsCommandInput {
  command: string;
  scope: string;
  operator: string;
}

function createCommandId() {
  if (typeof crypto !== 'undefined' && 'randomUUID' in crypto) {
    return `cmd-${crypto.randomUUID()}`;
  }
  return `cmd-${Math.random().toString(36).slice(2, 10)}`;
}

export async function postOperationsCommand(
  input: PostOperationsCommandInput,
): Promise<CommandEvent> {
  const issuedAt = new Date().toISOString();

  return {
    id: createCommandId(),
    command: input.command,
    scope: input.scope,
    operator: input.operator,
    issuedAt,
    status: 'pending',
  } satisfies CommandEvent;
}
