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

type ResolvedIntegrationConfig = Partial<OpsIntegrationConfig>;

let cachedConfig: OpsIntegrationConfig | null = null;

export function isIntegrationConfigured(): boolean {
  if (cachedConfig) {
    return true;
  }

  const resolved = resolveIntegrationConfig();
  return Boolean(resolved.managementBaseUrl && resolved.gatewayBaseUrl && resolved.bearerToken);
}

export function getIntegrationConfig(): OpsIntegrationConfig {
  if (cachedConfig) {
    return cachedConfig;
  }

  const { managementBaseUrl, gatewayBaseUrl, bearerToken, tenantId } = resolveIntegrationConfig();

  if (!managementBaseUrl) {
    throw new Error(
      'Management base URL is not configured. Set OPS_MANAGEMENT_BASE_URL, MANAGEMENT_BASE_URL, or AZURE_FUNCTIONS_URL.',
    );
  }

  if (!gatewayBaseUrl) {
    throw new Error(
      'Gateway base URL is not configured. Set OPS_GATEWAY_BASE_URL or GATEWAY_BASE_URL.',
    );
  }

  if (!bearerToken) {
    throw new Error('OPS_BEARER_TOKEN is not configured.');
  }

  cachedConfig = {
    managementBaseUrl: stripTrailingSlash(managementBaseUrl),
    gatewayBaseUrl: stripTrailingSlash(gatewayBaseUrl),
    bearerToken,
    tenantId: tenantId ?? 'console',
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

function resolveIntegrationConfig(
  globalsOverride?: OpsIntegrationGlobals,
): ResolvedIntegrationConfig {
  const globals = globalsOverride ?? (globalThis as OpsIntegrationGlobals);
  const env = globals.process?.env ?? {};

  return {
    managementBaseUrl:
      globals.__OPS_MANAGEMENT_BASE_URL__ ??
      env.MANAGEMENT_BASE_URL ??
      env.OPS_MANAGEMENT_BASE_URL ??
      env.AZURE_FUNCTIONS_URL ??
      env.VITE_AZURE_FUNCTIONS_URL ??
      env.VITE_MANAGEMENT_BASE_URL,
    gatewayBaseUrl:
      globals.__OPS_GATEWAY_BASE_URL__ ?? env.GATEWAY_BASE_URL ?? env.OPS_GATEWAY_BASE_URL,
    bearerToken:
      globals.__OPS_BEARER_TOKEN__ ??
      env.OPS_BEARER_TOKEN ??
      env.MANAGEMENT_BEARER_TOKEN ??
      env.BEARER_TOKEN,
    tenantId:
      globals.__OPS_TENANT_ID__ ??
      env.OPS_TENANT_ID ??
      env.MANAGEMENT_TENANT_ID ??
      env.TRADE_AGENT_TENANT ??
      'console',
  };
}
