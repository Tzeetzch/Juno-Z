import { test, expect } from '@playwright/test';

/**
 * Smoke test - just verify the server is running
 */

test('server responds', async ({ page }) => {
  const response = await page.goto('/');
  expect(response?.status()).toBeLessThan(500);
  await page.waitForLoadState('networkidle');
});

test('should redirect to login', async ({ page }) => {
  await page.goto('/');
  await page.waitForLoadState('networkidle');
  await expect(page).toHaveURL(/login/i);
});
