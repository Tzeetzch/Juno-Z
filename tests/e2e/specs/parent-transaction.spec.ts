import { test, expect } from '@playwright/test';

/**
 * Phase E Cycle 3: Manual Transaction Form
 * Updated for Phase J: Multi-child support - access via child detail page
 * 
 * MudBlazor-specific handling for Blazor Server app
 * IMPORTANT: Use in-app navigation (clicking buttons) instead of page.goto() 
 * to preserve Blazor Server session state
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

// Helper function to navigate to a child's transaction page
async function navigateToChildTransaction(page: any, childName: string) {
  // Click on child card
  await page.locator(`.child-card:has-text("${childName}")`).click();
  await expect(page).toHaveURL(/\/parent\/child\/\d+/, { timeout: 10000 });
  await page.waitForLoadState('networkidle');
  
  // Click Add/Remove Money button on child detail page
  await page.locator('button:has-text("Add/Remove Money")').click();
  await expect(page).toHaveURL(/\/parent\/child\/\d+\/transaction/, { timeout: 10000 });
  await page.waitForLoadState('networkidle');
}

test.describe('Parent Manual Transaction', () => {

  test('should show manual transaction form for child', async ({ page }) => {
    await loginAsParent(page);
    await navigateToChildTransaction(page, 'Junior');

    // Check for child context header
    await expect(page.getByText(/Managing: Junior/i)).toBeVisible({ timeout: 10000 });

    // Check page title
    await expect(page.getByText('💰 Add or Remove Money')).toBeVisible();
    
    // Check for radio buttons (MudRadio renders as label text)
    await expect(page.getByText('Add Money (Deposit)')).toBeVisible();
    await expect(page.getByText('Remove Money (Withdrawal)')).toBeVisible();
    
    // Check for form fields
    await expect(page.locator('.mud-input-control').filter({ hasText: 'Amount' })).toBeVisible();
    await expect(page.locator('.mud-input-control').filter({ hasText: 'Description' })).toBeVisible();
    
    // Check for submit button
    await expect(page.locator('button:has-text("Submit")')).toBeVisible();
  });

  test('should add money successfully', async ({ page }) => {
    await loginAsParent(page);
    await navigateToChildTransaction(page, 'Junior');

    // Deposit is default, just fill the form
    await page.locator('.mud-input-control').filter({ hasText: 'Amount' }).locator('input').fill('5.00');
    await page.locator('.mud-input-control').filter({ hasText: 'Description' }).locator('textarea').fill('Chores reward');
    await page.locator('button:has-text("Submit")').click();

    // Should show success snackbar
    await expect(page.getByText(/added successfully/i)).toBeVisible({ timeout: 10000 });
  });

  test('should remove money successfully', async ({ page }) => {
    await loginAsParent(page);
    await navigateToChildTransaction(page, 'Junior');

    // Select withdrawal by clicking the radio label
    await page.getByText('Remove Money (Withdrawal)').click();
    
    await page.locator('.mud-input-control').filter({ hasText: 'Amount' }).locator('input').fill('1.00');
    await page.locator('.mud-input-control').filter({ hasText: 'Description' }).locator('textarea').fill('Spent at store');
    await page.locator('button:has-text("Submit")').click();

    // Should show success snackbar
    await expect(page.getByText(/removed successfully/i)).toBeVisible({ timeout: 10000 });
  });

  test('should navigate to child detail via child card', async ({ page }) => {
    await loginAsParent(page);

    // Click on Junior's card
    await page.locator('.child-card:has-text("Junior")').click();
    await expect(page).toHaveURL(/\/parent\/child\/\d+/, { timeout: 10000 });
  });

  test('should navigate back to child detail from transaction page', async ({ page }) => {
    await loginAsParent(page);
    await navigateToChildTransaction(page, 'Junior');

    // Use back link
    await page.getByText(/← Back/i).click();
    await expect(page).toHaveURL(/\/parent\/child\/\d+$/, { timeout: 10000 });
  });
});
