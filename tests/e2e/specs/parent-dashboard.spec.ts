import { test, expect } from '@playwright/test';

/**
 * Phase E Cycle 1: Parent Dashboard
 * 
 * MudBlazor-specific handling for Blazor Server app
 */

// Helper function to login as parent
async function loginAsParent(page: any) {
  await page.goto('/login/parent');
  await page.waitForLoadState('networkidle');
  
  await page.locator('.mud-input-control').filter({ hasText: 'Email' }).locator('input').fill('dad@junobank.local');
  await page.locator('.mud-input-control').filter({ hasText: 'Password' }).locator('input').fill('parent123');
  await page.locator('button.neu-btn:has-text("Login")').click();
  
  await expect(page).toHaveURL(/\/parent/, { timeout: 15000 });
  await page.waitForLoadState('networkidle');
}

test.describe('Parent Dashboard', () => {

  test('should show child balance and pending request count', async ({ page }) => {
    await loginAsParent(page);

    // Should show welcome message
    await expect(page.getByText(/Welcome, Dad/i)).toBeVisible({ timeout: 10000 });

    // Should show child's balance card (contains "Balance")
    await expect(page.getByText(/Balance/i)).toBeVisible();

    // Should show pending requests count
    await expect(page.getByText(/Pending Requests/i)).toBeVisible();
  });

  test('should have quick action buttons', async ({ page }) => {
    await loginAsParent(page);

    // Check for all quick action buttons with their actual text including emojis
    await expect(page.locator('button:has-text("Review Requests")')).toBeVisible();
    await expect(page.locator('button:has-text("Add/Remove Money")')).toBeVisible();
    await expect(page.locator('button:has-text("Transaction History")')).toBeVisible();
    await expect(page.locator('button:has-text("Settings")')).toBeVisible();
  });

  test('should navigate to pending requests', async ({ page }) => {
    await loginAsParent(page);

    await page.locator('button:has-text("Review Requests")').click();
    await expect(page).toHaveURL('/parent/requests', { timeout: 10000 });
  });

  test('should navigate to manual transaction page', async ({ page }) => {
    await loginAsParent(page);

    await page.locator('button:has-text("Add/Remove Money")').click();
    await expect(page).toHaveURL('/parent/transaction', { timeout: 10000 });
  });

  test('should navigate to transaction history', async ({ page }) => {
    await loginAsParent(page);

    await page.locator('button:has-text("Transaction History")').click();
    await expect(page).toHaveURL('/parent/history', { timeout: 10000 });
  });

  test('should navigate to settings', async ({ page }) => {
    await loginAsParent(page);

    await page.locator('button:has-text("Settings")').click();
    await expect(page).toHaveURL('/parent/settings', { timeout: 10000 });
  });
});
