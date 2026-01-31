import { test, expect } from '@playwright/test';

/**
 * Phase E Cycle 4: Parent Transaction History
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

test.describe('Parent Transaction History', () => {

  test('should show transaction history page', async ({ page }) => {
    await loginAsParent(page);
    
    // Navigate using dashboard button instead of page.goto to preserve session
    await page.locator('button:has-text("Transaction History")').click();
    await page.waitForLoadState('networkidle');

    await expect(page.getByText('📜 Transaction History')).toBeVisible({ timeout: 10000 });
  });

  test('should navigate from dashboard', async ({ page }) => {
    await loginAsParent(page);

    await page.locator('button:has-text("Transaction History")').click();
    await expect(page).toHaveURL('/parent/history', { timeout: 10000 });
  });

  test('should show transactions after manual add', async ({ page }) => {
    await loginAsParent(page);

    // Navigate to transaction using dashboard button to preserve session
    await page.locator('button:has-text("Add/Remove Money")').click();
    await page.waitForLoadState('networkidle');
    
    await page.locator('.mud-input-control').filter({ hasText: 'Amount' }).locator('input').fill('7.50');
    await page.locator('.mud-input-control').filter({ hasText: 'Description' }).locator('textarea').fill('History test deposit');
    await page.locator('button:has-text("Submit")').click();
    await expect(page.getByText(/added successfully/i)).toBeVisible({ timeout: 10000 });

    // Navigate to history using back button then dashboard
    await page.getByText('← Back to Dashboard').click();
    await page.waitForLoadState('networkidle');
    await page.locator('button:has-text("Transaction History")').click();
    await page.waitForLoadState('networkidle');
    
    await expect(page.getByText('History test deposit')).toBeVisible({ timeout: 10000 });
  });

  test('should navigate back to dashboard', async ({ page }) => {
    await loginAsParent(page);
    
    // Navigate using dashboard button
    await page.locator('button:has-text("Transaction History")').click();
    await page.waitForLoadState('networkidle');

    await page.getByText('← Back to Dashboard').click();
    await expect(page).toHaveURL('/parent', { timeout: 10000 });
  });

  test('should show initial seed transaction', async ({ page }) => {
    await loginAsParent(page);
    
    // Navigate using dashboard button
    await page.locator('button:has-text("Transaction History")').click();
    await page.waitForLoadState('networkidle');

    // Should show the seed data transaction "Welcome to Juno Bank"
    await expect(page.getByText(/Welcome to Juno Bank/i)).toBeVisible({ timeout: 10000 });
  });
});
