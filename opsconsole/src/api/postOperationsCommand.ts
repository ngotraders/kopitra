import type { CommandEvent } from '../types/console.ts';
import { expectManagementOk, managementRequest } from './integration/config.ts';

export interface PostOperationsCommandInput {
  command: string;
  scope: string;
  operator: string;
}

export async function postOperationsCommand(
  input: PostOperationsCommandInput,
): Promise<CommandEvent> {
  const response = await managementRequest('/opsconsole/commands', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  });
  await expectManagementOk(response);
  const payload = (await response.json()) as CommandEvent;
  return payload;
}
