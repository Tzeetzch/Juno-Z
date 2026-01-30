import { test, expect } from '@playwright/test';

/**
 * Phase E Cycle 1: Parent Dashboard
 */

test.describe('Parent Dashboard', () => {

  test('should show child balance and pending request count', async ({ page }) => {
    // Login as parent
    await page.goto('/login/parent');
    await page.getByLabel(/Email/i).fill('dad@junobank.local');
    await page.getByLabel(/Password/i).fill('parent123');
    await page.locator('button:has-text("Login")').click();

    // Should end up on parent dashboard
    await page.waitForURL('/parent');

    // Should show welcome message
    await expect(page.getByText(/Welcome, Dad/i)).toBeVisible();

    // Should show child's balance card
    await expect(page.getByText(/Balance/i)).toBeVisible();

    // Should show pending requests count
    await expect(page.getByText(/Pending Requests/i)).toBeVisible();
  });

  test('should have quick action buttons', async ({ page }) => {
    await page.goto('/login/parent');
    await page.getByLabel(/Email/i).fill('dad@junobank.local');
    await page.getByLabel(/Password/i).fill('parent123');
    await page.locator('button:has-text("Login")').click();
    await page.waitForURL('/parent');

    await expect(page.getByRole('button', { name: /Review Requests/i })).toBeVisible();
    await expect(page.getByRole('button', { name: /Add\/Remove Money/i })).toBeVisible();
    await expect(page.getByRole('button', { name: /Transaction History/i })).toBeVisible();
    await expect(page.getByRole('button', { name: /Settings/i })).toBeVisible();
  });

  test('should navigate to pending requests', async ({ page }) => {
    await page.goto('/login/parent');
    await page.getByLabel(/Email/i).fill('dad@junobank.local');
    await page.getByLabel(/Password/i).fill('parent123');
    await page.locator('button:has-text("Login")').click();
    await page.waitForURL('/parent');

    await page.getByRole('button', { name: /Review Requests/i }).click();
    await expect(page).toHaveURL('/parent/requests');
  });
});
