import { loadEnv } from 'vite';
import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');
  const azureFunctionsUrl =
    env.AZURE_FUNCTIONS_URL ?? env.VITE_AZURE_FUNCTIONS_URL ?? 'http://localhost:7071/api';

  return {
    define: {
      'import.meta.env.VITE_AZURE_FUNCTIONS_URL': JSON.stringify(azureFunctionsUrl),
    },
    plugins: [react()],
    test: {
      environment: 'jsdom',
      globals: true,
      setupFiles: ['./src/test/setup.ts'],
    },
  };
});
