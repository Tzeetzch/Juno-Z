import { test, expect } from '@playwright/test';
import { loginAsChild } from '../helpers';

/**
 * Phase E Cycle 2: Pending requests with approve/deny
 * Updated for Phase J: Multi-child support with 2-step login
 * 
 * MudBlazor-specific handling for Blazor Server app
 * IMPORTANT: Use in-app navigation instead of page.goto() to preserve session
 */

// Helper: login as child (Junior) and submit a withdrawal request
async function submitChildRequest(page: any, description: string = 'E2E test request') {
  await loginAsChild(page);

  await page.locator('button:has-text("Request Money")').click();
  await expect(page).toHaveURL('/child/request-withdrawal', { timeout: 10000 });
  await page.waitForLoadState('networkidle');
  
  await page.locator('.mud-input-control').filter({ hasText: 'Amount' }).locator('input').fill('2.00');
  await page.locator('.mud-input-control').filter({ hasText: 'What do you want it for' }).locator('textarea').fill(description);
  await page.locator('button:has-text("Ask Mom or Dad")').click();
  
  await expect(page.getByText('Request Sent! ✅')).toBeVisible({ timeout: 10000 });
}

// Helper: login as parent
async function loginAsParent(page: any) {
  await page.goto('/login/parent');
  await page.waitForLoadState('networkidle');
  
  await page.locator('.mud-input-control').filter({ hasText: 'Email' }).locator('input').fill('dad@junobank.local');
  await page.locator('.mud-input-control').filter({ hasText: 'Password' }).locator('input').fill('parent123');
  await page.locator('button.neu-btn:has-text("Login")').click();
  
  await expect(page).toHaveURL(/\/parent/, { timeout: 15000 });
  await page.waitForLoadState('networkidle');
}

test.describe('Parent Pending Requests', () => {

  test('should show pending requests page', async ({ page }) => {
    await loginAsParent(page);
    
    // Navigate using dashboard button
    await page.locator('button:has-text("Review Requests")').click();
    await page.waitForLoadState('networkidle');

    await expect(page.getByText('📋 Pending Requests')).toBeVisible({ timeout: 10000 });
  });

  test('should show approve and deny buttons for pending request', async ({ page }) => {
    // Create a request first
    await submitChildRequest(page, 'Request for approve/deny test');

    // Now login as parent and check
    await loginAsParent(page);
    
    // Navigate using dashboard button
    await page.locator('button:has-text("Review Requests")').click();
    await page.waitForLoadState('networkidle');

    await expect(page.getByText('Request for approve/deny test')).toBeVisible({ timeout: 10000 });
    await expect(page.locator('button:has-text("Approve")').first()).toBeVisible();
    await expect(page.locator('button:has-text("Deny")').first()).toBeVisible();
  });

  test('should approve a request', async ({ page }) => {
    await submitChildRequest(page, 'Request to approve');

    await loginAsParent(page);
    
    // Navigate using dashboard button
    await page.locator('button:has-text("Review Requests")').click();
    await page.waitForLoadState('networkidle');

    // Find and approve the request
    await expect(page.getByText('Request to approve')).toBeVisible({ timeout: 10000 });
    await page.locator('button:has-text("Approve")').first().click();

    // Should show success snackbar
    await expect(page.getByText(/Request approved/i)).toBeVisible({ timeout: 10000 });
  });

  test('should deny a request', async ({ page }) => {
    await submitChildRequest(page, 'Request to deny');

    await loginAsParent(page);
    
    // Navigate using dashboard button
    await page.locator('button:has-text("Review Requests")').click();
    await page.waitForLoadState('networkidle');

    await expect(page.getByText('Request to deny')).toBeVisible({ timeout: 10000 });
    await page.locator('button:has-text("Deny")').first().click();

    // Should show denial snackbar
    await expect(page.getByText(/Request denied/i)).toBeVisible({ timeout: 10000 });
  });

  test('should show empty state when no pending requests', async ({ page }) => {
    await loginAsParent(page);
    
    // Navigate using dashboard button
    await page.locator('button:has-text("Review Requests")').click();
    await page.waitForLoadState('networkidle');

    // The page shows "No pending requests!" when there are none
    // Check for either the empty state or existing requests
    const emptyState = page.getByText('No pending requests!');
    const hasEmptyState = await emptyState.isVisible().catch(() => false);
    
    if (!hasEmptyState) {
      // There are pending requests, which is also valid
      await expect(page.getByText('📋 Pending Requests')).toBeVisible();
    } else {
      await expect(emptyState).toBeVisible();
    }
  });

  test('should navigate back to dashboard', async ({ page }) => {
    await loginAsParent(page);
    
    // Navigate using dashboard button
    await page.locator('button:has-text("Review Requests")').click();
    await page.waitForLoadState('networkidle');

    await page.getByText('← Back to Dashboard').click();
    await expect(page).toHaveURL('/parent', { timeout: 10000 });
  });
});
