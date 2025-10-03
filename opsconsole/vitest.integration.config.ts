import { defineConfig } from 'vitest/config';

export default defineConfig({
  test: {
    environment: 'node',
    globals: true,
    include: ['src/api/integration/**/*.integration.test.ts'],
    threads: false,
    testTimeout: 120_000,
    hookTimeout: 120_000,
    sequence: {
      shuffle: false,
    },
  },
});
