import { test, expect } from '@playwright/test';

/**
 * Phase E Cycle 5: Settings Page (Allowance Config)
 */

test.describe('Parent Settings', () => {

  async function loginAsParent(page: any) {
    await page.goto('/login/parent');
    await page.getByLabel(/Email/i).fill('dad@junobank.local');
    await page.getByLabel(/Password/i).fill('parent123');
    await page.locator('button:has-text("Login")').click();
    await page.waitForURL('/parent');
  }

  test('should show settings page with allowance config', async ({ page }) => {
    await loginAsParent(page);
    await page.goto('/parent/settings');

    await expect(page.getByText(/Settings/i).first()).toBeVisible();
    await expect(page.getByText(/Weekly Allowance/i)).toBeVisible();
    await expect(page.getByText(/Enable weekly allowance/i)).toBeVisible();
    await expect(page.getByLabel(/Amount per week/i)).toBeVisible();
  });

  test('should save allowance settings', async ({ page }) => {
    await loginAsParent(page);
    await page.goto('/parent/settings');

    // Enable allowance
    await page.getByText(/Enable weekly allowance/i).click();

    // Set amount
    await page.getByLabel(/Amount per week/i).fill('5.00');

    // Save
    await page.getByRole('button', { name: /Save Settings/i }).click();

    // Should show success
    await expect(page.getByText(/Settings saved/i)).toBeVisible();
  });

  test('should navigate from dashboard', async ({ page }) => {
    await loginAsParent(page);

    await page.getByRole('button', { name: /Settings/i }).click();
    await expect(page).toHaveURL('/parent/settings');
  });
});
