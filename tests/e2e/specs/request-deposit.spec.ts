import { test, expect } from '@playwright/test';
import { loginAsChild } from '../helpers';

/**
 * Cycle 4: Request Deposit Form
 * Updated for Phase J: Multi-child support with 2-step login
 * 
 * MudBlazor-specific handling for Blazor Server app
 */

test.describe('Request Deposit', () => {

  test('should show request deposit form', async ({ page }) => {
    await loginAsChild(page);

    // Click "Add Money" button
    await page.locator('button:has-text("Add Money")').click();

    // Should navigate to request deposit page
    await expect(page).toHaveURL('/child/request-deposit', { timeout: 10000 });
    await page.waitForLoadState('networkidle');

    // Check form elements
    await expect(page.getByText('🎁 Add Money')).toBeVisible();
    await expect(page.locator('.mud-input-control').filter({ hasText: 'Amount' })).toBeVisible();
    await expect(page.locator('.mud-input-control').filter({ hasText: 'Where did it come from' })).toBeVisible();
    await expect(page.locator('button:has-text("Ask Mom or Dad")')).toBeVisible();
  });

  test('should submit deposit request successfully', async ({ page }) => {
    await loginAsChild(page);

    // Go to request deposit
    await page.locator('button:has-text("Add Money")').click();
    await expect(page).toHaveURL('/child/request-deposit', { timeout: 10000 });
    await page.waitForLoadState('networkidle');

    // Fill form using MudBlazor selectors
    await page.locator('.mud-input-control').filter({ hasText: 'Amount' }).locator('input').fill('10.00');
    await page.locator('.mud-input-control').filter({ hasText: 'Where did it come from' }).locator('textarea').fill('Birthday money from grandma');

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
    await page.locator('button:has-text("Add Money")').click();
    await expect(page).toHaveURL('/child/request-deposit', { timeout: 10000 });
    await page.waitForLoadState('networkidle');

    // Fill and submit
    await page.locator('.mud-input-control').filter({ hasText: 'Amount' }).locator('input').fill('5.00');
    await page.locator('.mud-input-control').filter({ hasText: 'Where did it come from' }).locator('textarea').fill('Found in my pocket');
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
    await page.locator('button:has-text("Add Money")').click();
    await expect(page).toHaveURL('/child/request-deposit', { timeout: 10000 });
    await page.waitForLoadState('networkidle');

    // Click cancel
    await page.locator('button:has-text("Cancel")').click();

    // Should return to dashboard
    await expect(page).toHaveURL('/child', { timeout: 10000 });
  });
});
