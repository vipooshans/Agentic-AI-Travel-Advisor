import { defineConfig, loadEnv } from 'vite';
import { configDefaults } from 'vitest/config';
import react from '@vitejs/plugin-react';

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');
  // Same-origin /api requests are forwarded to the ASP.NET Core API, so no CORS is needed in dev or preview.
  const apiTarget = env.VITE_API_PROXY_TARGET || 'http://localhost:5080';
  const proxy = { '/api': { target: apiTarget, changeOrigin: true } };

  return {
    plugins: [react()],
    server: { port: 5173, proxy },
    preview: { port: 4173, proxy },
    test: {
      globals: true,
      environment: 'jsdom',
      setupFiles: ['./src/test/setup.ts'],
      // Playwright specs run against the real API with `npm run e2e`, not in jsdom.
      exclude: [...configDefaults.exclude, 'e2e/**'],
      css: false,
      restoreMocks: true,
    },
  };
});
