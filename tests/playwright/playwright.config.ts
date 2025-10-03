import { defineConfig } from '@playwright/test';

const opsconsoleBaseUrl = process.env.OPSCONSOLE_BASE_URL ?? 'http://localhost:4173';
const gatewayBaseUrl = process.env.GATEWAY_BASE_URL ?? 'http://localhost:8080';
const managementBaseUrl = process.env.MANAGEMENT_BASE_URL ?? 'http://localhost:7071/api';

export default defineConfig({
  testDir: './tests',
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  workers: 1,
  timeout: 180_000,
  use: {
    baseURL: opsconsoleBaseUrl,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },
  reporter: [['list'], ['html', { outputFolder: 'playwright-report', open: 'never' }]],
  metadata: {
    opsconsoleBaseUrl,
    gatewayBaseUrl,
    managementBaseUrl,
  },
});
