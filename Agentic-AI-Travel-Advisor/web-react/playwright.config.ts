import { defineConfig, devices } from '@playwright/test';

const baseURL = process.env.E2E_BASE_URL ?? 'http://localhost:5173';
const apiURL = process.env.E2E_API_URL ?? 'http://localhost:5080';

/**
 * Browser tests against the real stack: Vite dev server -> ASP.NET Core API -> PostgreSQL.
 * Nothing is mocked. Set E2E_BASE_URL / E2E_API_URL to point at another deployment (e.g. Docker Compose),
 * in which case no local servers are started.
 */
export default defineConfig({
  testDir: './e2e',
  // Specs share one database and create real hotels, packages and bookings, so run them in order.
  fullyParallel: false,
  workers: 1,
  retries: 0,
  timeout: 60_000,
  expect: { timeout: 10_000 },
  reporter: [
    ['list'],
    ['junit', { outputFile: 'test-results/junit.xml' }],
    ['html', { open: 'never', outputFolder: 'playwright-report' }],
  ],
  use: {
    baseURL,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    video: 'off',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: process.env.E2E_BASE_URL
    ? undefined
    : [
        {
          // The suite signs in ~30 times; the default of 10 auth requests/minute/IP is covered by
          // SecurityHardeningTests. An API that is already running must be started with the same setting.
          command: 'dotnet run --project ../web-api --no-build --urls http://localhost:5080 --environment Development',
          url: `${apiURL}/api/settings/public`,
          reuseExistingServer: true,
          timeout: 120_000,
          env: { RateLimiting__AuthPermitsPerMinute: '300' },
        },
        {
          command: 'npm run dev',
          url: baseURL,
          reuseExistingServer: true,
          timeout: 60_000,
        },
      ],
});
