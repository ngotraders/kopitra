import type { ConsoleUser } from '../types/console.ts';
import {
  expectManagementOk,
  getIntegrationConfig,
  managementRequest,
} from './integration/config.ts';

export interface OpsConsoleLoginRequest {
  userId: string;
  email: string;
}

export interface OpsConsoleLoginResponse {
  token: string;
  issuedAt: string;
  user: ConsoleUser;
}

export async function postOpsConsoleLogin(
  request: OpsConsoleLoginRequest,
): Promise<OpsConsoleLoginResponse> {
  await getIntegrationConfig();
  const response = await managementRequest('/opsconsole/login', {
    method: 'POST',
    body: JSON.stringify({
      userId: request.userId.trim(),
      email: request.email.trim(),
    }),
  });
  await expectManagementOk(response);
  return (await response.json()) as OpsConsoleLoginResponse;
}
