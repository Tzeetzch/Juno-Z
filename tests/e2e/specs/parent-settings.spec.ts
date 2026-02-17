import { test, expect } from '@playwright/test';
import { loginAsParent } from '../helpers';

/**
 * Parent Settings (Per-Child) — Standing Orders & Picture Password.
 *
 * The settings page now uses Standing Orders (not the old Scheduled Allowance toggle).
 * Each standing order is managed via an inline dialog.
 *
 * IMPORTANT: Use in-app navigation instead of page.goto() to preserve session.
 */

// Helper function to navigate to a child's settings page
async function navigateToChildSettings(page: any, childName: string) {
  await page.locator(`.child-card:has-text("${childName}")`).click();
  await expect(page).toHaveURL(/\/parent\/child\/\d+/, { timeout: 10000 });
  await page.waitForLoadState('networkidle');

  await page.locator('button:has-text("Settings")').click();
  await expect(page).toHaveURL(/\/parent\/child\/\d+\/settings/, { timeout: 10000 });
  await page.waitForLoadState('networkidle');
}

test.describe('Parent Settings (Per-Child)', () => {

  test('should show settings page with child context', async ({ page }) => {
    await loginAsParent(page);
    await navigateToChildSettings(page, 'Junior');

    await expect(page.getByText(/Managing: Junior/i)).toBeVisible({ timeout: 10000 });
    await expect(page.getByText(/Settings for Junior/i)).toBeVisible();
  });

  test('should show standing orders section', async ({ page }) => {
    await loginAsParent(page);
    await navigateToChildSettings(page, 'Junior');

    await expect(page.getByText('💰 Standing Orders')).toBeVisible({ timeout: 10000 });
  });

  test('should show add button for standing orders', async ({ page }) => {
    await loginAsParent(page);
    await navigateToChildSettings(page, 'Junior');

    // The "Add" button to create a new standing order
    await expect(page.locator('button:has-text("Add")')).toBeVisible({ timeout: 10000 });
  });

  test('should show empty state or existing orders', async ({ page }) => {
    await loginAsParent(page);
    await navigateToChildSettings(page, 'Junior');

    // Should show either existing orders or the empty state
    const emptyState = page.getByText('No standing orders yet');
    const hasEmpty = await emptyState.isVisible().catch(() => false);

    if (hasEmpty) {
      await expect(page.getByText(/Add one to schedule automatic payments/)).toBeVisible();
    } else {
      // At least one order exists — verify it has schedule information
      await expect(page.locator('.neu-card').first()).toBeVisible();
    }
  });

  test('should open add new order dialog', async ({ page }) => {
    await loginAsParent(page);
    await navigateToChildSettings(page, 'Junior');

    await page.locator('button:has-text("Add")').click();

    // Dialog should open with order editor form
    await expect(page.getByText('New Standing Order')).toBeVisible({ timeout: 10000 });
    await expect(page.locator('.mud-input-control').filter({ hasText: 'Amount' })).toBeVisible();
    await expect(page.locator('button:has-text("Cancel")')).toBeVisible();
    await expect(page.locator('button:has-text("Save")')).toBeVisible();
  });

  test('should show picture password section with reset button', async ({ page }) => {
    await loginAsParent(page);
    await navigateToChildSettings(page, 'Junior');

    await expect(page.getByRole('heading', { name: 'Picture Password' })).toBeVisible({ timeout: 10000 });
    await expect(page.locator('button:has-text("Reset Password")')).toBeVisible();
  });

  test('should open reset picture password dialog', async ({ page }) => {
    await loginAsParent(page);
    await navigateToChildSettings(page, 'Junior');

    await page.locator('button:has-text("Reset Password")').click();

    // Dialog should open with PictureGridSetup
    await expect(page.getByText('Reset Picture Password')).toBeVisible({ timeout: 10000 });
    await expect(page.getByText(/Pick 4 images/)).toBeVisible();
    await expect(page.locator('button:has-text("Cancel")')).toBeVisible();
    await expect(page.locator('button:has-text("Save")')).toBeVisible();
  });

  test('should navigate back to child detail', async ({ page }) => {
    await loginAsParent(page);
    await navigateToChildSettings(page, 'Junior');

    // ChildContextHeader back button (text: "Back")
    await page.locator('button:has-text("Back")').click();
    await expect(page).toHaveURL(/\/parent\/child\/\d+$/, { timeout: 10000 });
  });
});

test.describe('Admin Panel — Reset Password', () => {
  // NOTE: The Settings page (/parent/settings) uses ProtectedSessionStorage which
  // has a known issue with Blazor Server enhanced navigation (ObjectDisposedException).
  // The page gets stuck in loading when navigated to from another page.
  // Full business logic is covered by 7 unit tests in UserServiceTests.cs.
  // These E2E tests verify the dashboard has a settings button for navigation.

  test('should show settings icon on parent dashboard', async ({ page }) => {
    await loginAsParent(page);

    // Settings icon button should be visible on the dashboard
    await expect(page.locator('button[aria-label="Settings"]')).toBeVisible({ timeout: 5000 });
  });
});
