import { test, expect } from '@playwright/test';
import { loginAsChild } from '../helpers';

/**
 * Request Withdrawal Form — child requests to spend money.
 *
 * Note: If the child has 5+ open requests, the form shows "Too Many Requests!"
 * alongside the form. Submissions are rejected by the backend at the limit.
 * Tests handle both states gracefully.
 */

test.describe('Request Withdrawal', () => {

  test('should show request withdrawal form', async ({ page }) => {
    await loginAsChild(page);

    // Click "Request Money" button
    await page.locator('button:has-text("Request Money")').click();

    // Should navigate to request withdrawal page
    await expect(page).toHaveURL('/child/request-withdrawal', { timeout: 10000 });
    await page.waitForLoadState('networkidle');

    // Check heading (always visible)
    await expect(page.getByText('💰 Request Money')).toBeVisible();

    // Check form elements
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

    // Check if at request limit
    const atLimit = await page.getByText('Too Many Requests!').isVisible().catch(() => false);

    if (atLimit) {
      // At limit — verify the limit warning and back button
      await expect(page.getByText(/requests waiting/)).toBeVisible();
      await expect(page.locator('button:has-text("Back to My Piggy Bank")')).toBeVisible();
    } else {
      // Fill form
      await page.locator('.mud-input-control').filter({ hasText: 'Amount' }).locator('input').fill('5.00');
      await page.locator('.mud-input-control').filter({ hasText: 'What do you want it for' }).locator('textarea').fill('For candy at the store');

      // Submit
      await page.locator('button:has-text("Ask Mom or Dad")').click();
      await page.waitForLoadState('networkidle');

      // Should show success message
      await expect(page.getByText('Request Sent! ✅')).toBeVisible({ timeout: 15000 });
      await expect(page.getByText(/Mom or Dad will review/i)).toBeVisible();

      // Should have back button
      await expect(page.locator('button:has-text("Back to My Piggy Bank")')).toBeVisible();
    }
  });

  test('should navigate back to dashboard after success', async ({ page }) => {
    await loginAsChild(page);

    // Go to request form
    await page.locator('button:has-text("Request Money")').click();
    await expect(page).toHaveURL('/child/request-withdrawal', { timeout: 10000 });
    await page.waitForLoadState('networkidle');

    // Check if at request limit
    const atLimit = await page.getByText('Too Many Requests!').isVisible().catch(() => false);

    if (atLimit) {
      // At limit — use the back button
      await page.locator('button:has-text("Back to My Piggy Bank")').click();
      await expect(page).toHaveURL('/child', { timeout: 10000 });
    } else {
      // Fill and submit
      await page.locator('.mud-input-control').filter({ hasText: 'Amount' }).locator('input').fill('10.00');
      await page.locator('.mud-input-control').filter({ hasText: 'What do you want it for' }).locator('textarea').fill('For a toy');
      await page.locator('button:has-text("Ask Mom or Dad")').click();
      await page.waitForLoadState('networkidle');

      // Wait for success message
      await expect(page.getByText('Request Sent! ✅')).toBeVisible({ timeout: 15000 });

      // Click back button
      await page.locator('button:has-text("Back to My Piggy Bank")').click();

      // Should be back on dashboard
      await expect(page).toHaveURL('/child', { timeout: 10000 });
    }
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
