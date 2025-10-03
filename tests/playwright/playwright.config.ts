import { defineConfig } from '@playwright/test';

const gatewayBaseUrl = process.env.GATEWAY_BASE_URL ?? 'http://localhost:8080';
const managementBaseUrl = process.env.MANAGEMENT_BASE_URL ?? 'http://localhost:7071/api';

export default defineConfig({
  testDir: './tests',
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  workers: 1,
  timeout: 120_000,
  use: {
    baseURL: gatewayBaseUrl,
    extraHTTPHeaders: {
      'Accept': 'application/json',
    },
  },
  reporter: [['list'], ['html', { outputFolder: 'playwright-report', open: 'never' }]],
  metadata: {
    gatewayBaseUrl,
    managementBaseUrl,
  },
});
