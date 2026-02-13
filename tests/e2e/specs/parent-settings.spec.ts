import { test, expect } from '@playwright/test';
import { loginAsParent } from '../helpers';

/**
 * Parent Settings (Per-Child) — Standing Orders & Picture Password.
 *
 * The settings page now uses Standing Orders (not the old Scheduled Allowance toggle).
 * Each standing order is managed via a separate order editor page.
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

  test('should navigate to add new order', async ({ page }) => {
    await loginAsParent(page);
    await navigateToChildSettings(page, 'Junior');

    await page.locator('button:has-text("Add")').click();
    await expect(page).toHaveURL(/\/parent\/child\/\d+\/order\/new/, { timeout: 10000 });
  });

  test('should show picture password section', async ({ page }) => {
    await loginAsParent(page);
    await navigateToChildSettings(page, 'Junior');

    await expect(page.getByText('🖼️ Picture Password')).toBeVisible({ timeout: 10000 });
    await expect(page.getByText(/Coming soon/i)).toBeVisible();
  });

  test('should navigate back to child detail', async ({ page }) => {
    await loginAsParent(page);
    await navigateToChildSettings(page, 'Junior');

    // ChildContextHeader back button (text: "Back")
    await page.locator('button:has-text("Back")').click();
    await expect(page).toHaveURL(/\/parent\/child\/\d+$/, { timeout: 10000 });
  });
});
