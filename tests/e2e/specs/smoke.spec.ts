import { test, expect } from '@playwright/test';

/**
 * Smoke test - just verify the server is running
 */

test('server responds', async ({ page }) => {
  const response = await page.goto('/');
  expect(response?.status()).toBeLessThan(500);
  console.log('Server is responding!');
  console.log('URL:', page.url());
  console.log('Title:', await page.title());
});
