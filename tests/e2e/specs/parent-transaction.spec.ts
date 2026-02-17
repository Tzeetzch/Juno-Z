import { test, expect } from '@playwright/test';
import { loginAsParent } from '../helpers';

/**
 * Parent Manual Transaction Form (per-child via child detail page).
 *
 * MudBlazor-specific handling for Blazor Server app.
 * IMPORTANT: Use in-app navigation instead of page.goto() to preserve session.
 */

// Helper function to open the transaction dialog for a child
async function openTransactionDialog(page: any, childName: string) {
  // Click on child card
  await page.locator(`.child-card:has-text("${childName}")`).click();
  await expect(page).toHaveURL(/\/parent\/child\/\d+/, { timeout: 10000 });
  await page.waitForLoadState('networkidle');

  // Click Add/Remove Money button — opens dialog instead of navigating
  await page.locator('button:has-text("Add/Remove Money")').click();
  await expect(page.getByText('Add or Remove Money')).toBeVisible({ timeout: 10000 });
}

test.describe('Parent Manual Transaction', () => {

  test('should show transaction dialog with form fields', async ({ page }) => {
    await loginAsParent(page);
    await openTransactionDialog(page, 'Junior');

    // Check for radio buttons
    await expect(page.getByText('Add Money (Deposit)')).toBeVisible();
    await expect(page.getByText('Remove Money (Withdrawal)')).toBeVisible();

    // Check for form fields
    await expect(page.locator('.mud-input-control').filter({ hasText: 'Amount' })).toBeVisible();
    await expect(page.locator('.mud-input-control').filter({ hasText: 'Description' })).toBeVisible();

    // Check for dialog buttons
    await expect(page.locator('button:has-text("Cancel")')).toBeVisible();
    await expect(page.locator('button:has-text("Submit")')).toBeVisible();
  });

  test('should add money successfully', async ({ page }) => {
    await loginAsParent(page);
    await openTransactionDialog(page, 'Junior');

    // Deposit is default, just fill the form
    await page.locator('.mud-input-control').filter({ hasText: 'Amount' }).locator('input').fill('5.00');
    await page.locator('.mud-input-control').filter({ hasText: 'Description' }).locator('textarea').fill('Chores reward');
    await page.locator('button:has-text("Submit")').click();

    // Should show success snackbar: "€5.00 added to Junior's balance!"
    await expect(page.getByText(/€[\d.]+ added to .+'s balance/i)).toBeVisible({ timeout: 10000 });
  });

  test('should remove money successfully', async ({ page }) => {
    await loginAsParent(page);
    await openTransactionDialog(page, 'Junior');

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

  test('should close dialog on cancel', async ({ page }) => {
    await loginAsParent(page);
    await openTransactionDialog(page, 'Junior');

    await page.locator('button:has-text("Cancel")').click();

    // Dialog should close, still on child detail page
    await expect(page.getByText('Add or Remove Money').first()).toBeHidden({ timeout: 5000 });
    await expect(page).toHaveURL(/\/parent\/child\/\d+$/, { timeout: 5000 });
  });
});
