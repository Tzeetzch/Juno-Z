import { test, expect } from '@playwright/test';
import { loginAsChild } from '../helpers';

/**
 * Cycle 5: Visual feedback & pending requests on child dashboard
 * Updated for Phase J: Multi-child support with 2-step login
 * 
 * MudBlazor-specific handling for Blazor Server app
 */

test.describe('Child Requests & Visual Feedback', () => {

  test('should show pending request on dashboard after withdrawal submit', async ({ page }) => {
    await loginAsChild(page);

    // Navigate to withdrawal request form
    await page.locator('button:has-text("Request Money")').click();
    await expect(page).toHaveURL('/child/request-withdrawal', { timeout: 10000 });
    await page.waitForLoadState('networkidle');

    // Fill the form using MudBlazor selectors
    await page.locator('.mud-input-control').filter({ hasText: 'Amount' }).locator('input').fill('3.00');
    await page.locator('.mud-input-control').filter({ hasText: 'What do you want it for' }).locator('textarea').fill('Test withdrawal for e2e');
    
    // Submit the form
    await page.locator('button:has-text("Ask Mom or Dad")').click();

    // Wait for success message
    await expect(page.getByText('Request Sent! ✅')).toBeVisible({ timeout: 10000 });

    // Go back to dashboard
    await page.locator('button:has-text("Back to My Piggy Bank")').click();
    await expect(page).toHaveURL('/child', { timeout: 10000 });
    await page.waitForLoadState('networkidle');

    // Should see "My Requests" section with the pending request
    await expect(page.getByText('📋 My Requests')).toBeVisible({ timeout: 10000 });
    await expect(page.getByText('Test withdrawal for e2e')).toBeVisible();
  });

  test('should show pending request on dashboard after deposit submit', async ({ page }) => {
    await loginAsChild(page);

    // Navigate to deposit request form
    await page.locator('button:has-text("Add Money")').click();
    await expect(page).toHaveURL('/child/request-deposit', { timeout: 10000 });
    await page.waitForLoadState('networkidle');

    // Fill the form
    await page.locator('.mud-input-control').filter({ hasText: 'Amount' }).locator('input').fill('15.00');
    await page.locator('.mud-input-control').filter({ hasText: 'Where did it come from' }).locator('textarea').fill('Birthday money from grandma');
    
    // Submit
    await page.locator('button:has-text("Ask Mom or Dad")').click();

    // Wait for success
    await expect(page.getByText('Request Sent! ✅')).toBeVisible({ timeout: 10000 });

    // Go back to dashboard
    await page.locator('button:has-text("Back to My Piggy Bank")').click();
    await expect(page).toHaveURL('/child', { timeout: 10000 });
    await page.waitForLoadState('networkidle');

    // Should see the deposit request
    await expect(page.getByText('Birthday money from grandma')).toBeVisible({ timeout: 10000 });
  });

  test('should show success alert after request submit', async ({ page }) => {
    await loginAsChild(page);

    // Navigate to withdrawal request
    await page.locator('button:has-text("Request Money")').click();
    await expect(page).toHaveURL('/child/request-withdrawal', { timeout: 10000 });
    await page.waitForLoadState('networkidle');

    // Fill and submit
    await page.locator('.mud-input-control').filter({ hasText: 'Amount' }).locator('input').fill('2.00');
    await page.locator('.mud-input-control').filter({ hasText: 'What do you want it for' }).locator('textarea').fill('Snackbar test');
    await page.locator('button:has-text("Ask Mom or Dad")').click();

    // Should see success alert with review message
    await expect(page.getByText('Request Sent! ✅')).toBeVisible({ timeout: 10000 });
    await expect(page.getByText(/Mom or Dad will review/i)).toBeVisible();
  });
});
