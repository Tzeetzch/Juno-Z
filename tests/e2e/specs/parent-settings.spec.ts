import { test, expect } from '@playwright/test';

/**
 * Phase F: Settings Page (Allowance Config)
 * 
 * MudBlazor-specific handling for Blazor Server app
 * IMPORTANT: Use in-app navigation instead of page.goto() to preserve session
 * 
 * See E2E_CONTEXT.md for critical Blazor session handling rules.
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

test.describe('Parent Settings', () => {

  test('should show settings page with all allowance config fields', async ({ page }) => {
    await loginAsParent(page);
    
    // Navigate using dashboard button
    await page.locator('button:has-text("Settings")').click();
    await page.waitForLoadState('networkidle');

    await expect(page.getByText('⚙️ Settings')).toBeVisible({ timeout: 10000 });
    await expect(page.getByRole('heading', { name: 'Weekly Allowance' })).toBeVisible();
    await expect(page.getByText('Enable weekly allowance')).toBeVisible();
    await expect(page.locator('.mud-input-control').filter({ hasText: 'Amount per week' })).toBeVisible();
    await expect(page.locator('.mud-input-control').filter({ hasText: 'Description' })).toBeVisible();
    await expect(page.locator('.mud-input-control').filter({ hasText: 'Day of week' })).toBeVisible();
    await expect(page.locator('.mud-input-control').filter({ hasText: 'Time of day' })).toBeVisible();
  });

  test('should save allowance settings with description', async ({ page }) => {
    await loginAsParent(page);
    
    // Navigate using dashboard button
    await page.locator('button:has-text("Settings")').click();
    await page.waitForLoadState('networkidle');

    // Enable allowance by clicking the switch label
    await page.getByText('Enable weekly allowance').click();

    // Set amount
    await page.locator('.mud-input-control').filter({ hasText: 'Amount per week' }).locator('input').fill('5.00');

    // Set description
    await page.locator('.mud-input-control').filter({ hasText: 'Description' }).locator('input').fill('Pocket Money');

    // Save
    await page.locator('button:has-text("Save Settings")').click();

    // Should show success snackbar
    await expect(page.getByText(/Settings saved/i)).toBeVisible({ timeout: 10000 });
  });

  test('should show next allowance preview when enabled', async ({ page }) => {
    await loginAsParent(page);
    
    // Navigate using dashboard button
    await page.locator('button:has-text("Settings")').click();
    await page.waitForLoadState('networkidle');

    // Check if allowance is already enabled from previous test
    const switchElement = page.locator('.mud-switch');
    const isChecked = await switchElement.locator('input').isChecked();
    
    // Only enable if not already enabled
    if (!isChecked) {
      await page.getByText('Enable weekly allowance').click();
    }
    
    // Wait for amount field to become enabled (Blazor re-rendering)
    const amountInput = page.locator('.mud-input-control').filter({ hasText: 'Amount per week' }).locator('input');
    await expect(amountInput).toBeEnabled({ timeout: 5000 });

    // Set amount and save to trigger preview update
    await amountInput.fill('5.00');
    await page.locator('button:has-text("Save Settings")').click();
    await page.waitForLoadState('networkidle');

    // Should show next allowance preview
    await expect(page.getByText(/Next allowance:/i)).toBeVisible({ timeout: 10000 });
  });

  test('should navigate from dashboard', async ({ page }) => {
    await loginAsParent(page);

    await page.locator('button:has-text("Settings")').click();
    await expect(page).toHaveURL('/parent/settings', { timeout: 10000 });
  });

  test('should navigate back to dashboard', async ({ page }) => {
    await loginAsParent(page);
    
    // Navigate using dashboard button
    await page.locator('button:has-text("Settings")').click();
    await page.waitForLoadState('networkidle');

    await page.getByText('← Back to Dashboard').click();
    await expect(page).toHaveURL('/parent', { timeout: 10000 });
  });

  test('should disable fields when allowance is off', async ({ page }) => {
    await loginAsParent(page);
    
    // Navigate using dashboard button
    await page.locator('button:has-text("Settings")').click();
    await page.waitForLoadState('networkidle');

    // Ensure allowance is disabled (click if currently enabled)
    const switchElement = page.locator('.mud-switch');
    const isChecked = await switchElement.locator('input').isChecked();
    if (isChecked) {
      await page.getByText('Enable weekly allowance').click();
      await page.waitForTimeout(300);
    }

    // Amount field should be disabled
    const amountInput = page.locator('.mud-input-control').filter({ hasText: 'Amount per week' }).locator('input');
    await expect(amountInput).toBeDisabled();

    // Description field should be disabled
    const descInput = page.locator('.mud-input-control').filter({ hasText: 'Description' }).locator('input');
    await expect(descInput).toBeDisabled();
  });
});
