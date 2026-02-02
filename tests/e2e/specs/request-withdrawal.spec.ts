import { test, expect } from '@playwright/test';
import { loginAsChild } from '../helpers';

/**
 * Cycle 3: Request Withdrawal Form
 * Updated for Phase J: Multi-child support with 2-step login
 * 
 * MudBlazor-specific handling for Blazor Server app
 */

test.describe('Request Withdrawal', () => {

  test('should show request withdrawal form', async ({ page }) => {
    await loginAsChild(page);

    // Click "Request Money" button
    await page.locator('button:has-text("Request Money")').click();

    // Should navigate to request withdrawal page
    await expect(page).toHaveURL('/child/request-withdrawal', { timeout: 10000 });
    await page.waitForLoadState('networkidle');

    // Check form elements
    await expect(page.getByText('💰 Request Money')).toBeVisible();
    await expect(page.locator('.mud-input-control').filter({ hasText: 'Amount' })).toBeVisible();
    await expect(page.locator('.mud-input-control').filter({ hasText: 'What do you want it for' })).toBeVisible();
    await expect(page.locator('button:has-text("Ask Mom or Dad")')).toBeVisible();
  });

  test('should submit withdrawal request successfully', async ({ page }) => {
    await loginAsChild(page);

    // Go to request withdrawal
    await page.locator('button:has-text("Request Money")').click();
    await expect(page).toHaveURL('/child/request-withdrawal', { timeout: 10000 });
    await page.waitForLoadState('networkidle');

    // Fill form using MudBlazor selectors
    await page.locator('.mud-input-control').filter({ hasText: 'Amount' }).locator('input').fill('5.00');
    await page.locator('.mud-input-control').filter({ hasText: 'What do you want it for' }).locator('textarea').fill('For candy at the store');

    // Submit
    await page.locator('button:has-text("Ask Mom or Dad")').click();

    // Should show success message
    await expect(page.getByText('Request Sent! ✅')).toBeVisible({ timeout: 10000 });
    await expect(page.getByText(/Mom or Dad will review/i)).toBeVisible();

    // Should have back button
    await expect(page.locator('button:has-text("Back to My Piggy Bank")')).toBeVisible();
  });

  test('should navigate back to dashboard after success', async ({ page }) => {
    await loginAsChild(page);

    // Go to request form
    await page.locator('button:has-text("Request Money")').click();
    await expect(page).toHaveURL('/child/request-withdrawal', { timeout: 10000 });
    await page.waitForLoadState('networkidle');

    // Fill and submit
    await page.locator('.mud-input-control').filter({ hasText: 'Amount' }).locator('input').fill('10.00');
    await page.locator('.mud-input-control').filter({ hasText: 'What do you want it for' }).locator('textarea').fill('For a toy');
    await page.locator('button:has-text("Ask Mom or Dad")').click();

    // Wait for success message
    await expect(page.getByText('Request Sent! ✅')).toBeVisible({ timeout: 10000 });

    // Click back button
    await page.locator('button:has-text("Back to My Piggy Bank")').click();

    // Should be back on dashboard
    await expect(page).toHaveURL('/child', { timeout: 10000 });
  });

  test('should show cancel button that returns to dashboard', async ({ page }) => {
    await loginAsChild(page);

    // Go to request form
    await page.locator('button:has-text("Request Money")').click();
    await expect(page).toHaveURL('/child/request-withdrawal', { timeout: 10000 });
    await page.waitForLoadState('networkidle');

    // Click cancel
    await page.locator('button:has-text("Cancel")').click();

    // Should return to dashboard
    await expect(page).toHaveURL('/child', { timeout: 10000 });
  });
});
