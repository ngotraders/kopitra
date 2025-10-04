import { afterEach, describe, expect, it, vi } from 'vitest';

const ORIGINAL_ENV = { ...process.env };

describe('integration config environment resolution', () => {
  afterEach(() => {
    process.env = { ...ORIGINAL_ENV };
    vi.resetModules();
  });

  it('prefers AZURE_FUNCTIONS_URL when resolving the management base URL', async () => {
    process.env.AZURE_FUNCTIONS_URL = 'https://example.azurewebsites.net/api/';
    process.env.OPS_GATEWAY_BASE_URL = 'https://gateway.example.com';
    process.env.OPS_BEARER_TOKEN = 'token';

    const { getIntegrationConfig } = await import('./config.ts');
    const config = getIntegrationConfig();

    expect(config.managementBaseUrl).toBe('https://example.azurewebsites.net/api');
  });
});
