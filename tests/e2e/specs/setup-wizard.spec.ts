import { test, expect } from '@playwright/test';

/**
 * Setup Wizard Smoke Tests
 *
 * The e2e test infrastructure seeds demo data (admin exists),
 * so the wizard should redirect to login. Full wizard flow
 * is covered by 11 unit tests for SetupService.
 */
test.describe('Setup Wizard', () => {

  test('redirects to login when admin already exists', async ({ page }) => {
    await page.goto('/setup');
    await page.waitForLoadState('networkidle');

    // Should redirect away from /setup since admin exists
    await expect(page).not.toHaveURL(/\/setup/, { timeout: 10000 });

    // Should end up at login or parent dashboard
    const url = page.url();
    expect(url.includes('/login') || url.includes('/parent')).toBeTruthy();
  });

  test('setup route is not accessible when app is already configured', async ({ page }) => {
    await page.goto('/setup');
    await page.waitForLoadState('networkidle');

    // The setup wizard page content should not be visible
    await expect(page.getByText("Let's set up Juno Bank")).not.toBeVisible({ timeout: 5000 });
  });

});
