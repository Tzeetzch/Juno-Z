import { test, expect } from '@playwright/test';

/**
 * Cycle 1: Child Dashboard + Balance Display
 * 
 * MudBlazor-specific handling for Blazor Server app
 */

// Helper function to login as child
async function loginAsChild(page: any) {
  await page.goto('/login/child');
  await page.waitForLoadState('networkidle');
  
  // Click the picture sequence: cat→dog→star→moon
  await page.locator('.picture-btn:has-text("🐱")').click();
  await page.locator('.picture-btn:has-text("🐶")').click();
  await page.locator('.picture-btn:has-text("⭐")').click();
  await page.locator('.picture-btn:has-text("🌙")').click();
  
  // Wait for navigation to child dashboard
  await expect(page).toHaveURL(/\/child/, { timeout: 15000 });
  await page.waitForLoadState('networkidle');
}

test.describe('Child Dashboard', () => {

  test('should show child dashboard after login', async ({ page }) => {
    await loginAsChild(page);

    // Should be on /child dashboard
    await expect(page).toHaveURL('/child');
  });

  test('should display child name and balance', async ({ page }) => {
    await loginAsChild(page);

    // Check for welcome message with name (Hi [Name]! 👋)
    await expect(page.getByText(/Hi.*!/)).toBeVisible({ timeout: 10000 });

    // Check for balance display label
    await expect(page.getByText('🐷 Piggy Bank Balance')).toBeVisible();

    // Check for "My Money" heading (exact match to avoid matching "My Money History")
    await expect(page.getByRole('heading', { name: 'My Money', exact: true })).toBeVisible();
  });

  test('should be protected - redirect to login if not authenticated', async ({ page }) => {
    // Try to access /child without logging in
    await page.goto('/child');
    await page.waitForLoadState('networkidle');

    // Should redirect to login
    await expect(page).toHaveURL(/login/);
  });

  test('should have navigation buttons to request forms', async ({ page }) => {
    await loginAsChild(page);

    // Check for "Request Money" button (💰 Request Money)
    await expect(page.locator('button:has-text("Request Money")')).toBeVisible();

    // Check for "Add Money" button (🎁 Add Money)
    await expect(page.locator('button:has-text("Add Money")')).toBeVisible();
  });

  test('should display transaction history section', async ({ page }) => {
    await loginAsChild(page);

    // Check for transaction history section heading
    await expect(page.getByText('📜 My Money History')).toBeVisible({ timeout: 10000 });

    // Check for initial deposit transaction (from seed data)
    await expect(page.getByText(/Welcome to Juno Bank/i)).toBeVisible();
  });
});
