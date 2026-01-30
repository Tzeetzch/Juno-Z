import { test, expect } from '@playwright/test';

/**
 * Phase E Cycle 3: Manual Transaction Form
 */

test.describe('Parent Manual Transaction', () => {

  async function loginAsParent(page: any) {
    await page.goto('/login/parent');
    await page.getByLabel(/Email/i).fill('dad@junobank.local');
    await page.getByLabel(/Password/i).fill('parent123');
    await page.locator('button:has-text("Login")').click();
    await page.waitForURL('/parent');
  }

  test('should show manual transaction form', async ({ page }) => {
    await loginAsParent(page);
    await page.goto('/parent/transaction');

    await expect(page.getByText(/Add or Remove Money/i)).toBeVisible();
    await expect(page.getByText(/Add Money/i)).toBeVisible();
    await expect(page.getByText(/Remove Money/i)).toBeVisible();
    await expect(page.getByLabel(/Amount/i)).toBeVisible();
    await expect(page.getByLabel(/Description/i)).toBeVisible();
  });

  test('should add money successfully', async ({ page }) => {
    await loginAsParent(page);
    await page.goto('/parent/transaction');

    // Deposit is default
    await page.getByLabel(/Amount/i).fill('5.00');
    await page.getByLabel(/Description/i).fill('Chores reward');
    await page.getByRole('button', { name: /Submit/i }).click();

    // Should show success snackbar
    await expect(page.getByText(/added successfully/i)).toBeVisible();
  });

  test('should remove money successfully', async ({ page }) => {
    await loginAsParent(page);
    await page.goto('/parent/transaction');

    // Select withdrawal
    await page.getByText(/Remove Money/i).click();
    await page.getByLabel(/Amount/i).fill('1.00');
    await page.getByLabel(/Description/i).fill('Spent at store');
    await page.getByRole('button', { name: /Submit/i }).click();

    // Should show success snackbar
    await expect(page.getByText(/removed successfully/i)).toBeVisible();
  });

  test('should navigate from dashboard', async ({ page }) => {
    await loginAsParent(page);

    await page.getByRole('button', { name: /Add\/Remove Money/i }).click();
    await expect(page).toHaveURL('/parent/transaction');
  });
});
