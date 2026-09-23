import { defineConfig, devices } from '@playwright/test';

/**
 * E2E suite for the Ledger Dashboard happy path (docs/spec-frontEnd.md).
 *
 * The Angular dev server is started automatically (or reused locally) via
 * `webServer`; its proxy forwards /api to the backend at localhost:5001, which
 * must be running (`docker compose up --build` from the repo root).
 */
export default defineConfig({
  testDir: './e2e',
  // One shared backend: serialize tests so list counts and balances stay deterministic.
  fullyParallel: false,
  workers: 1,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: 'http://localhost:4200',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: {
    command: 'npm start',
    url: 'http://localhost:4200',
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
  },
});
