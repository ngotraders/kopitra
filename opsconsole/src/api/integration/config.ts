export interface OpsIntegrationConfig {
  managementBaseUrl: string;
  gatewayBaseUrl: string;
  bearerToken: string;
  tenantId: string;
}

interface OpsIntegrationGlobals {
  __OPS_MANAGEMENT_BASE_URL__?: string;
  __OPS_GATEWAY_BASE_URL__?: string;
  __OPS_BEARER_TOKEN__?: string;
  __OPS_TENANT_ID__?: string;
  process?: {
    env?: Record<string, string | undefined>;
  };
}

let cachedConfig: OpsIntegrationConfig | null = null;

export function getIntegrationConfig(): OpsIntegrationConfig {
  if (cachedConfig) {
    return cachedConfig;
  }

  const globals = globalThis as OpsIntegrationGlobals;
  const env = globals.process?.env ?? {};

  const managementBaseUrl =
    globals.__OPS_MANAGEMENT_BASE_URL__ ??
    env.MANAGEMENT_BASE_URL ??
    env.OPS_MANAGEMENT_BASE_URL ??
    env.VITE_MANAGEMENT_BASE_URL;

  if (!managementBaseUrl) {
    throw new Error(
      'Management base URL is not configured. Set OPS_MANAGEMENT_BASE_URL or MANAGEMENT_BASE_URL.',
    );
  }

  const gatewayBaseUrl =
    globals.__OPS_GATEWAY_BASE_URL__ ?? env.GATEWAY_BASE_URL ?? env.OPS_GATEWAY_BASE_URL;
  if (!gatewayBaseUrl) {
    throw new Error(
      'Gateway base URL is not configured. Set OPS_GATEWAY_BASE_URL or GATEWAY_BASE_URL.',
    );
  }

  const bearerToken =
    globals.__OPS_BEARER_TOKEN__ ??
    env.OPS_BEARER_TOKEN ??
    env.MANAGEMENT_BEARER_TOKEN ??
    env.BEARER_TOKEN;
  if (!bearerToken) {
    throw new Error('OPS_BEARER_TOKEN is not configured.');
  }

  const tenantId =
    globals.__OPS_TENANT_ID__ ??
    env.OPS_TENANT_ID ??
    env.MANAGEMENT_TENANT_ID ??
    env.TRADE_AGENT_TENANT ??
    'console';

  cachedConfig = {
    managementBaseUrl: stripTrailingSlash(managementBaseUrl),
    gatewayBaseUrl: stripTrailingSlash(gatewayBaseUrl),
    bearerToken,
    tenantId,
  };

  return cachedConfig;
}

export async function managementRequest(path: string, init: RequestInit = {}) {
  const config = getIntegrationConfig();
  const url = new URL(path, `${config.managementBaseUrl}/`).toString();
  const headers = new Headers(init.headers ?? {});

  if (!headers.has('Authorization')) {
    headers.set('Authorization', `Bearer ${config.bearerToken}`);
  }
  if (!headers.has('X-TradeAgent-Account')) {
    headers.set('X-TradeAgent-Account', config.tenantId);
  }
  if (!headers.has('Accept')) {
    headers.set('Accept', 'application/json');
  }
  if (init.body !== undefined && typeof init.body === 'string' && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json');
  }

  return fetch(url, { ...init, headers });
}

export async function gatewayRequest(path: string, init: RequestInit = {}) {
  const config = getIntegrationConfig();
  const url = new URL(path, `${config.gatewayBaseUrl}/`).toString();
  const headers = new Headers(init.headers ?? {});
  if (!headers.has('Accept')) {
    headers.set('Accept', 'application/json');
  }
  if (init.body !== undefined && typeof init.body === 'string' && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json');
  }

  return fetch(url, { ...init, headers });
}

async function parseErrorResponse(
  origin: 'management' | 'gateway',
  response: Response,
): Promise<never> {
  let detail: unknown = undefined;
  try {
    detail = await response.json();
  } catch {
    try {
      const text = await response.text();
      if (text) {
        detail = text;
      }
    } catch {
      /* ignore */
    }
  }
  throw new Error(
    `${origin} request failed (${response.status} ${response.statusText}): ${JSON.stringify(detail)}`,
  );
}

export async function expectManagementOk(response: Response) {
  if (!response.ok) {
    await parseErrorResponse('management', response);
  }
}

export async function expectGatewayOk(response: Response) {
  if (!response.ok) {
    await parseErrorResponse('gateway', response);
  }
}

function stripTrailingSlash(value: string): string {
  return value.replace(/\/+$/, '');
}
