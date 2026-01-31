import { defineConfig, devices } from '@playwright/test';
import { randomUUID } from 'crypto';

// Generate unique database name for this test run
const testRunId = randomUUID().substring(0, 8);
const testDbName = `junobank-test-${testRunId}.db`;

/**
 * Playwright configuration for Juno Bank
 * Optimized for text-based output to minimize token usage
 */
export default defineConfig({
  testDir: './specs',
  globalSetup: './global-setup.ts',  // Cleans up old test databases

  /* Maximum time one test can run for */
  timeout: 30 * 1000,

  /* Run tests serially to avoid session conflicts in Blazor Server */
  fullyParallel: false,

  /* Fail the build on CI if you accidentally left test.only in the source code */
  forbidOnly: !!process.env.CI,

  /* Retry on CI only */
  retries: process.env.CI ? 2 : 0,

  /* Single worker to avoid session state conflicts */
  workers: 1,

  /* Reporter configuration - TEXT ONLY by default */
  reporter: [
    ['list'],  // Detailed text output to console
    ['html', { open: 'never' }]  // HTML report for manual inspection (not auto-opened)
  ],

  /* Shared settings for all the projects below */
  use: {
    /* Base URL for tests */
    baseURL: 'http://localhost:5208',

    /* Collect trace ONLY on first retry (not on every run) */
    trace: 'on-first-retry',

    /* Screenshot ONLY on failure (not always) */
    screenshot: 'only-on-failure',

    /* Video DISABLED by default (very expensive) */
    video: 'off',
  },

  /* Configure projects for major browsers */
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },

    /* Uncomment to test on more browsers/viewports
    {
      name: 'firefox',
      use: { ...devices['Desktop Firefox'] },
    },
    {
      name: 'webkit',
      use: { ...devices['Desktop Safari'] },
    },
    {
      name: 'mobile-chrome',
      use: { ...devices['Pixel 5'] },
    },
    {
      name: 'tablet',
      use: { ...devices['iPad Pro'] },
    },
    */
  ],

  /* Run local dev server before starting tests */
  webServer: {
    command: `cmd /c "cd ../../src/JunoBank.Web && set JUNO_TEST_DB=${testDbName} && dotnet run --launch-profile http-test"`,
    url: 'http://localhost:5208',
    reuseExistingServer: false,  // Always start fresh server for tests
    timeout: 120 * 1000,
    stdout: 'pipe',
    stderr: 'pipe',
  },
});
