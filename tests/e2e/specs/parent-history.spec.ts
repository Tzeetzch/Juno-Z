import { test, expect } from '@playwright/test';

/**
 * Phase E Cycle 4: Parent Transaction History
 */

test.describe('Parent Transaction History', () => {

  async function loginAsParent(page: any) {
    await page.goto('/login/parent');
    await page.getByLabel(/Email/i).fill('dad@junobank.local');
    await page.getByLabel(/Password/i).fill('parent123');
    await page.locator('button:has-text("Login")').click();
    await page.waitForURL('/parent');
  }

  test('should show transaction history page', async ({ page }) => {
    await loginAsParent(page);
    await page.goto('/parent/history');

    await expect(page.getByText(/Transaction History/i)).toBeVisible();
  });

  test('should navigate from dashboard', async ({ page }) => {
    await loginAsParent(page);

    await page.getByRole('button', { name: /Transaction History/i }).click();
    await expect(page).toHaveURL('/parent/history');
  });

  test('should show transactions after manual add', async ({ page }) => {
    await loginAsParent(page);

    // Add a manual transaction first
    await page.goto('/parent/transaction');
    await page.getByLabel(/Amount/i).fill('7.50');
    await page.getByLabel(/Description/i).fill('History test deposit');
    await page.getByRole('button', { name: /Submit/i }).click();
    await expect(page.getByText(/added successfully/i)).toBeVisible();

    // Now check history
    await page.goto('/parent/history');
    await expect(page.getByText('History test deposit')).toBeVisible();
  });
});
