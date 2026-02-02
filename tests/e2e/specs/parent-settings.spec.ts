import { test, expect } from '@playwright/test';

/**
 * Phase F: Settings Page (Per-Child Allowance Config)
 * Updated for Phase J: Access via child detail page
 * 
 * MudBlazor-specific handling for Blazor Server app
 * IMPORTANT: Use in-app navigation instead of page.goto() to preserve session
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

// Helper function to navigate to a child's settings page
async function navigateToChildSettings(page: any, childName: string) {
  await page.locator(`.child-card:has-text("${childName}")`).click();
  await expect(page).toHaveURL(/\/parent\/child\/\d+/, { timeout: 10000 });
  await page.waitForLoadState('networkidle');
  
  await page.locator('button:has-text("Settings")').click();
  await expect(page).toHaveURL(/\/parent\/child\/\d+\/settings/, { timeout: 10000 });
  await page.waitForLoadState('networkidle');
}

test.describe('Parent Settings (Per-Child)', () => {

  test('should show settings page with child context', async ({ page }) => {
    await loginAsParent(page);
    await navigateToChildSettings(page, 'Junior');

    await expect(page.getByText(/Managing: Junior/i)).toBeVisible({ timeout: 10000 });
    await expect(page.getByText(/Settings for Junior/i)).toBeVisible();
  });

  test('should show all allowance config fields', async ({ page }) => {
    await loginAsParent(page);
    await navigateToChildSettings(page, 'Junior');

    await expect(page.getByText('Weekly Allowance')).toBeVisible();
    await expect(page.getByText('Enable weekly allowance')).toBeVisible();
    await expect(page.locator('.mud-input-control').filter({ hasText: 'Amount per week' })).toBeVisible();
    await expect(page.locator('.mud-input-control').filter({ hasText: 'Description' })).toBeVisible();
    await expect(page.locator('.mud-input-control').filter({ hasText: 'Day of week' })).toBeVisible();
    await expect(page.locator('.mud-input-control').filter({ hasText: 'Time of day' })).toBeVisible();
  });

  test('should save allowance settings successfully', async ({ page }) => {
    await loginAsParent(page);
    await navigateToChildSettings(page, 'Junior');

    // Enable allowance by clicking the switch label
    await page.getByText('Enable weekly allowance').click();

    // Set amount
    await page.locator('.mud-input-control').filter({ hasText: 'Amount per week' }).locator('input').fill('5.00');

    // Set description
    await page.locator('.mud-input-control').filter({ hasText: 'Description' }).locator('input').fill('Pocket Money');

    // Save
    await page.locator('button:has-text("Save Settings")').click();

    // Should show success snackbar
    await expect(page.getByText(/saved/i)).toBeVisible({ timeout: 10000 });
  });

  test('should show next allowance preview when enabled', async ({ page }) => {
    await loginAsParent(page);
    await navigateToChildSettings(page, 'Junior');

    // Check if allowance is already enabled
    const switchElement = page.locator('.mud-switch');
    const isChecked = await switchElement.locator('input').isChecked();
    
    if (!isChecked) {
      await page.getByText('Enable weekly allowance').click();
    }
    
    const amountInput = page.locator('.mud-input-control').filter({ hasText: 'Amount per week' }).locator('input');
    await expect(amountInput).toBeEnabled({ timeout: 5000 });
    await amountInput.fill('5.00');
    await page.locator('button:has-text("Save Settings")').click();
    await page.waitForLoadState('networkidle');

    await expect(page.getByText(/Next allowance:/i)).toBeVisible({ timeout: 10000 });
  });

  test('should navigate back to child detail', async ({ page }) => {
    await loginAsParent(page);
    await navigateToChildSettings(page, 'Junior');

    await page.getByText(/← Back/i).click();
    await expect(page).toHaveURL(/\/parent\/child\/\d+$/, { timeout: 10000 });
  });

  test('should disable fields when allowance is off', async ({ page }) => {
    await loginAsParent(page);
    await navigateToChildSettings(page, 'Junior');

    const switchElement = page.locator('.mud-switch');
    const isChecked = await switchElement.locator('input').isChecked();
    if (isChecked) {
      await page.getByText('Enable weekly allowance').click();
      await page.waitForTimeout(300);
    }

    const amountInput = page.locator('.mud-input-control').filter({ hasText: 'Amount per week' }).locator('input');
    await expect(amountInput).toBeDisabled();
  });
});
