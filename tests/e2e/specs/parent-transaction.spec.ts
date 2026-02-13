import { test, expect } from '@playwright/test';
import { loginAsParent } from '../helpers';

/**
 * Parent Manual Transaction Form (per-child via child detail page).
 *
 * MudBlazor-specific handling for Blazor Server app.
 * IMPORTANT: Use in-app navigation instead of page.goto() to preserve session.
 */

// Helper function to navigate to a child's transaction page
async function navigateToChildTransaction(page: any, childName: string) {
  // Click on child card
  await page.locator(`.child-card:has-text("${childName}")`).click();
  await expect(page).toHaveURL(/\/parent\/child\/\d+/, { timeout: 10000 });
  await page.waitForLoadState('networkidle');

  // Click Add/Remove Money button on child detail page
  await page.locator('button:has-text("Add/Remove Money")').click();
  await expect(page).toHaveURL(/\/parent\/child\/\d+\/transaction/, { timeout: 10000 });
  await page.waitForLoadState('networkidle');
}

test.describe('Parent Manual Transaction', () => {

  test('should show manual transaction form for child', async ({ page }) => {
    await loginAsParent(page);
    await navigateToChildTransaction(page, 'Junior');

    // Check for child context header
    await expect(page.getByText(/Managing: Junior/i)).toBeVisible({ timeout: 10000 });

    // Check page title
    await expect(page.getByText('💰 Add or Remove Money')).toBeVisible();

    // Check for radio buttons
    await expect(page.getByText('Add Money (Deposit)')).toBeVisible();
    await expect(page.getByText('Remove Money (Withdrawal)')).toBeVisible();

    // Check for form fields
    await expect(page.locator('.mud-input-control').filter({ hasText: 'Amount' })).toBeVisible();
    await expect(page.locator('.mud-input-control').filter({ hasText: 'Description' })).toBeVisible();

    // Check for submit button
    await expect(page.locator('button:has-text("Submit")')).toBeVisible();
  });

  test('should add money successfully', async ({ page }) => {
    await loginAsParent(page);
    await navigateToChildTransaction(page, 'Junior');

    // Deposit is default, just fill the form
    await page.locator('.mud-input-control').filter({ hasText: 'Amount' }).locator('input').fill('5.00');
    await page.locator('.mud-input-control').filter({ hasText: 'Description' }).locator('textarea').fill('Chores reward');
    await page.locator('button:has-text("Submit")').click();

    // Should show success snackbar: "€5.00 added to Junior's balance!"
    await expect(page.getByText(/€[\d.]+ added to .+'s balance/i)).toBeVisible({ timeout: 10000 });
  });

  test('should remove money successfully', async ({ page }) => {
    await loginAsParent(page);
    await navigateToChildTransaction(page, 'Junior');

    // Select withdrawal by clicking the radio label
    await page.getByText('Remove Money (Withdrawal)').click();

    await page.locator('.mud-input-control').filter({ hasText: 'Amount' }).locator('input').fill('1.00');
    await page.locator('.mud-input-control').filter({ hasText: 'Description' }).locator('textarea').fill('Spent at store');
    await page.locator('button:has-text("Submit")').click();

    // Should show success snackbar: "€1.00 removed from Junior's balance!"
    await expect(page.getByText(/€[\d.]+ removed from .+'s balance/i)).toBeVisible({ timeout: 10000 });
  });

  test('should navigate to child detail via child card', async ({ page }) => {
    await loginAsParent(page);

    // Click on Junior's card
    await page.locator('.child-card:has-text("Junior")').click();
    await expect(page).toHaveURL(/\/parent\/child\/\d+/, { timeout: 10000 });
  });

  test('should navigate back to child detail from transaction page', async ({ page }) => {
    await loginAsParent(page);
    await navigateToChildTransaction(page, 'Junior');

    // Use ChildContextHeader back button (text is "Back" with ArrowBack icon)
    await page.locator('button:has-text("Back")').click();
    await expect(page).toHaveURL(/\/parent\/child\/\d+$/, { timeout: 10000 });
  });
});
