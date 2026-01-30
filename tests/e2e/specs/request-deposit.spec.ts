import { test, expect } from '@playwright/test';

/**
 * Cycle 4: Request Deposit Form
 */

test.describe('Request Deposit', () => {

  test('should show request deposit form', async ({ page }) => {
    // Login as child
    await page.goto('/login/child');
    await page.locator('button:has-text("🐱")').click();
    await page.locator('button:has-text("🐶")').click();
    await page.locator('button:has-text("⭐")').click();
    await page.locator('button:has-text("🌙")').click();

    // Should be on dashboard
    await page.waitForURL('/child');

    // Click "Add Money" button
    await page.getByRole('button', { name: /Add Money/i }).click();

    // Should navigate to request deposit page
    await expect(page).toHaveURL('/child/request-deposit');

    // Check form elements
    await expect(page.getByRole('heading', { name: /Add Money/i })).toBeVisible();
    await expect(page.getByLabel(/Amount/i)).toBeVisible();
    await expect(page.getByLabel(/Where did it come from/i)).toBeVisible();
  });

  test('should submit deposit request successfully', async ({ page }) => {
    // Login
    await page.goto('/login/child');
    await page.locator('button:has-text("🐱")').click();
    await page.locator('button:has-text("🐶")').click();
    await page.locator('button:has-text("⭐")').click();
    await page.locator('button:has-text("🌙")').click();
    await page.waitForURL('/child');

    // Go to request deposit
    await page.getByRole('button', { name: /Add Money/i }).click();
    await page.waitForURL('/child/request-deposit');

    // Fill form
    await page.getByLabel(/Amount/i).fill('10.00');
    await page.getByLabel(/Where did it come from/i).fill('Birthday money from grandma');

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

    await page.getByRole('button', { name: /Add Money/i }).click();
    await page.waitForURL('/child/request-deposit');

    await page.getByLabel(/Amount/i).fill('5.00');
    await page.getByLabel(/Where did it come from/i).fill('Found in my pocket');
    await page.getByRole('button', { name: /Ask Mom or Dad/i }).click();

    // Wait for success message
    await expect(page.getByText(/Request Sent/i)).toBeVisible();

    // Click back button
    await page.getByRole('button', { name: /Back to My Piggy Bank/i }).click();

    // Should be back on dashboard
    await expect(page).toHaveURL('/child');
  });
});
