import { test, expect } from '@playwright/test';

/**
 * Phase E Cycle 4: Parent Transaction History (per-child)
 * Updated for Phase J: Access via child detail page only
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

// Helper function to navigate to a child's page
async function navigateToChildDetail(page: any, childName: string) {
  await page.locator(`.child-card:has-text("${childName}")`).click();
  await expect(page).toHaveURL(/\/parent\/child\/\d+/, { timeout: 10000 });
  await page.waitForLoadState('networkidle');
}

test.describe('Parent Transaction History (Per-Child)', () => {

  test('should navigate to child history from child detail', async ({ page }) => {
    await loginAsParent(page);
    await navigateToChildDetail(page, 'Junior');

    // Click history button
    await page.locator('button:has-text("Request History")').click();
    await expect(page).toHaveURL(/\/parent\/child\/\d+\/history/, { timeout: 10000 });
  });

  test('should show child transaction history page', async ({ page }) => {
    await loginAsParent(page);
    await navigateToChildDetail(page, 'Junior');

    await page.locator('button:has-text("Request History")').click();
    await page.waitForLoadState('networkidle');

    // Should show history page with child context
    await expect(page.getByText(/Managing: Junior/i)).toBeVisible({ timeout: 10000 });
  });

  test('should show initial seed transaction', async ({ page }) => {
    await loginAsParent(page);
    await navigateToChildDetail(page, 'Junior');

    await page.locator('button:has-text("Request History")').click();
    await page.waitForLoadState('networkidle');

    // Should show the seed data transaction "Welcome to Juno Bank"
    await expect(page.getByText(/Welcome to Juno Bank/i)).toBeVisible({ timeout: 10000 });
  });

  test('should navigate back to child detail', async ({ page }) => {
    await loginAsParent(page);
    await navigateToChildDetail(page, 'Junior');

    await page.locator('button:has-text("Request History")').click();
    await page.waitForLoadState('networkidle');

    await page.getByText(/← Back/i).click();
    await expect(page).toHaveURL(/\/parent\/child\/\d+$/, { timeout: 10000 });
  });
});
