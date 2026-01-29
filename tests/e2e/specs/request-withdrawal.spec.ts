import { test, expect } from '@playwright/test';

/**
 * Cycle 3: Request Withdrawal Form
 */

test.describe('Request Withdrawal', () => {

  test('should show request withdrawal form', async ({ page }) => {
    // Login as child
    await page.goto('/login/child');
    await page.locator('button:has-text("🐱")').click();
    await page.locator('button:has-text("🐶")').click();
    await page.locator('button:has-text("⭐")').click();
    await page.locator('button:has-text("🌙")').click();

    // Should be on dashboard
    await page.waitForURL('/child');

    // Click "Request Money" button
    await page.getByRole('button', { name: /Request Money/i }).click();

    // Should navigate to request withdrawal page
    await expect(page).toHaveURL('/child/request-withdrawal');

    // Check form elements
    await expect(page.getByRole('heading', { name: /Request Money/i })).toBeVisible();
    await expect(page.getByLabel(/Amount/i)).toBeVisible();
    await expect(page.getByLabel(/What do you want it for/i)).toBeVisible();
  });

  test('should submit withdrawal request successfully', async ({ page }) => {
    // Login
    await page.goto('/login/child');
    await page.locator('button:has-text("🐱")').click();
    await page.locator('button:has-text("🐶")').click();
    await page.locator('button:has-text("⭐")').click();
    await page.locator('button:has-text("🌙")').click();
    await page.waitForURL('/child');

    // Go to request withdrawal
    await page.getByRole('button', { name: /Request Money/i }).click();
    await page.waitForURL('/child/request-withdrawal');

    // Fill form
    await page.getByLabel(/Amount/i).fill('5.00');
    await page.getByLabel(/What do you want it for/i).fill('For candy at the store');

    // Submit
    await page.getByRole('button', { name: /Ask Mom or Dad/i }).click();

    // Should show success message
    await expect(page.getByText(/Request Sent/i)).toBeVisible();
    await expect(page.getByText(/Mom or Dad will review/i)).toBeVisible();

    // Should have back button
    await expect(page.getByRole('button', { name: /Back to My Piggy Bank/i })).toBeVisible();
  });

  test('should navigate back to dashboard after success', async ({ page }) => {
    // Login and submit request
    await page.goto('/login/child');
    await page.locator('button:has-text("🐱")').click();
    await page.locator('button:has-text("🐶")').click();
    await page.locator('button:has-text("⭐")').click();
    await page.locator('button:has-text("🌙")').click();
    await page.waitForURL('/child');

    await page.getByRole('button', { name: /Request Money/i }).click();
    await page.waitForURL('/child/request-withdrawal');

    await page.getByLabel(/Amount/i).fill('10.00');
    await page.getByLabel(/What do you want it for/i).fill('For a toy');
    await page.getByRole('button', { name: /Ask Mom or Dad/i }).click();

    // Wait for success message
    await expect(page.getByText(/Request Sent/i)).toBeVisible();

    // Click back button
    await page.getByRole('button', { name: /Back to My Piggy Bank/i }).click();

    // Should be back on dashboard
    await expect(page).toHaveURL('/child');
  });
});
