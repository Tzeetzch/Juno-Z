import { test, expect } from '@playwright/test';

/**
 * Phase E Cycle 2: Pending requests with approve/deny
 */

test.describe('Parent Pending Requests', () => {

  // Helper: login as child and submit a withdrawal request
  async function submitChildRequest(page: any) {
    await page.goto('/login/child');
    await page.locator('button:has-text("🐱")').click();
    await page.locator('button:has-text("🐶")').click();
    await page.locator('button:has-text("⭐")').click();
    await page.locator('button:has-text("🌙")').click();
    await page.waitForURL('/child');

    await page.getByRole('button', { name: /Request Money/i }).click();
    await page.waitForURL('/child/request-withdrawal');
    await page.getByLabel(/Amount/i).fill('2.00');
    await page.getByLabel(/What do you want it for/i).fill('E2E test request');
    await page.getByRole('button', { name: /Ask Mom or Dad/i }).click();
    await expect(page.getByText(/Request Sent/i)).toBeVisible();
  }

  // Helper: login as parent
  async function loginAsParent(page: any) {
    await page.goto('/login/parent');
    await page.getByLabel(/Email/i).fill('dad@junobank.local');
    await page.getByLabel(/Password/i).fill('parent123');
    await page.locator('button:has-text("Login")').click();
    await page.waitForURL('/parent');
  }

  test('should show pending requests page', async ({ page }) => {
    await loginAsParent(page);
    await page.goto('/parent/requests');

    await expect(page.getByText(/Pending Requests/i)).toBeVisible();
  });

  test('should show approve and deny buttons for pending request', async ({ page }) => {
    // Create a request first
    await submitChildRequest(page);

    // Now login as parent and check
    await loginAsParent(page);
    await page.goto('/parent/requests');

    await expect(page.getByText('E2E test request')).toBeVisible();
    await expect(page.getByRole('button', { name: /Approve/i })).toBeVisible();
    await expect(page.getByRole('button', { name: /Deny/i })).toBeVisible();
  });

  test('should approve a request', async ({ page }) => {
    await submitChildRequest(page);

    await loginAsParent(page);
    await page.goto('/parent/requests');

    // Find and approve the request
    await expect(page.getByText('E2E test request')).toBeVisible();
    await page.getByRole('button', { name: /Approve/i }).first().click();

    // Should show success snackbar
    await expect(page.getByText(/Request approved/i)).toBeVisible();
  });

  test('should deny a request', async ({ page }) => {
    await submitChildRequest(page);

    await loginAsParent(page);
    await page.goto('/parent/requests');

    await expect(page.getByText('E2E test request')).toBeVisible();
    await page.getByRole('button', { name: /Deny/i }).first().click();

    // Should show denial snackbar
    await expect(page.getByText(/Request denied/i)).toBeVisible();
  });
});
