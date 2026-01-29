import { test, expect } from '@playwright/test';

/**
 * Cycle 1: Child Dashboard + Balance Display
 */

test.describe('Child Dashboard', () => {

  test('should show child dashboard after login', async ({ page }) => {
    // Go to child login
    await page.goto('/login/child');

    // Login with correct sequence: cat→dog→star→moon
    await page.locator('button:has-text("🐱")').click();
    await page.locator('button:has-text("🐶")').click();
    await page.locator('button:has-text("⭐")').click();
    await page.locator('button:has-text("🌙")').click();

    // Should redirect to /child dashboard
    await expect(page).toHaveURL('/child');
  });

  test('should display child name and balance', async ({ page }) => {
    // Login first
    await page.goto('/login/child');
    await page.locator('button:has-text("🐱")').click();
    await page.locator('button:has-text("🐶")').click();
    await page.locator('button:has-text("⭐")').click();
    await page.locator('button:has-text("🌙")').click();

    // Wait for dashboard to load
    await page.waitForURL('/child');

    // Check for welcome message with name
    await expect(page.getByRole('heading', { name: /Hi Junior/i })).toBeVisible();

    // Check for balance display
    await expect(page.getByText('🐷 Piggy Bank Balance')).toBeVisible();

    // Check balance amount (should be €10.00 from seed data)
    //await expect(page.getByText(/10.00/)).toBeVisible();
  });

  test('should be protected - redirect to login if not authenticated', async ({ page }) => {
    // Try to access /child without logging in
    await page.goto('/child');

    // Should redirect to login
    await expect(page).toHaveURL(/login/);
  });

  test('should display transaction history', async ({ page }) => {
    // Login first
    await page.goto('/login/child');
    await page.locator('button:has-text("🐱")').click();
    await page.locator('button:has-text("🐶")').click();
    await page.locator('button:has-text("⭐")').click();
    await page.locator('button:has-text("🌙")').click();

    // Wait for dashboard
    await page.waitForURL('/child');

    // Check for transaction history section
    await expect(page.getByText(/My Money History/i)).toBeVisible();

    // Check for initial deposit transaction
    await expect(page.getByText(/Welcome to Juno Bank/i)).toBeVisible();

    // Check for positive amount display (deposit)
    await expect(page.getByText(/\+€10\.00/)).toBeVisible();

    // Check for deposit icon
    await expect(page.getByText('💰')).toBeVisible();
  });
});
