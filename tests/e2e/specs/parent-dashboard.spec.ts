import { test, expect } from '@playwright/test';

/**
 * Phase E Cycle 1: Parent Dashboard
 * Updated for Phase J: Multi-child support
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

  test('should show welcome message and children section', async ({ page }) => {
    await loginAsParent(page);

    // Should show welcome message
    await expect(page.getByText(/Welcome, Dad/i)).toBeVisible({ timeout: 10000 });

    // Should show "Your Children" section
    await expect(page.getByText(/Your Children/i)).toBeVisible();
  });

  test('should display multiple child cards', async ({ page }) => {
    await loginAsParent(page);

    // Should show Junior's card
    await expect(page.locator('.child-card:has-text("Junior")')).toBeVisible({ timeout: 10000 });

    // Should show Sophie's card
    await expect(page.locator('.child-card:has-text("Sophie")')).toBeVisible();
  });

  test('should show child balances on cards', async ({ page }) => {
    await loginAsParent(page);

    // Junior should have €10.00 (or some balance)
    const juniorCard = page.locator('.child-card:has-text("Junior")');
    await expect(juniorCard.locator('text=€')).toBeVisible();

    // Sophie should have €5.00 (or some balance)
    const sophieCard = page.locator('.child-card:has-text("Sophie")');
    await expect(sophieCard.locator('text=€')).toBeVisible();
  });

  test('should have quick action buttons', async ({ page }) => {
    await loginAsParent(page);

    // Check for quick action buttons (Add/Remove Money removed - now per-child)
    await expect(page.locator('button:has-text("Review Requests")')).toBeVisible();
    await expect(page.locator('button:has-text("Transaction History")')).toBeVisible();
    await expect(page.locator('button:has-text("Settings")')).toBeVisible();
  });

  test('should navigate to child detail when clicking child card', async ({ page }) => {
    await loginAsParent(page);

    // Click on Junior's card
    await page.locator('.child-card:has-text("Junior")').click();
    
    // Should navigate to child detail page
    await expect(page).toHaveURL(/\/parent\/child\/\d+/, { timeout: 10000 });
  });

  test('should navigate to pending requests', async ({ page }) => {
    await loginAsParent(page);

    await page.locator('button:has-text("Review Requests")').click();
    await expect(page).toHaveURL('/parent/requests', { timeout: 10000 });
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
