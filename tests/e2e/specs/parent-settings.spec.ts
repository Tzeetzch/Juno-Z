import { test, expect } from '@playwright/test';

/**
 * Phase E Cycle 5: Settings Page (Allowance Config)
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

test.describe('Parent Settings', () => {

  test('should show settings page with allowance config', async ({ page }) => {
    await loginAsParent(page);
    
    // Navigate using dashboard button
    await page.locator('button:has-text("Settings")').click();
    await page.waitForLoadState('networkidle');

    await expect(page.getByText('⚙️ Settings')).toBeVisible({ timeout: 10000 });
    await expect(page.getByRole('heading', { name: 'Weekly Allowance' })).toBeVisible();
    await expect(page.getByText('Enable weekly allowance')).toBeVisible();
    await expect(page.locator('.mud-input-control').filter({ hasText: 'Amount per week' })).toBeVisible();
  });

  test('should save allowance settings', async ({ page }) => {
    await loginAsParent(page);
    
    // Navigate using dashboard button
    await page.locator('button:has-text("Settings")').click();
    await page.waitForLoadState('networkidle');

    // Enable allowance by clicking the switch label
    await page.getByText('Enable weekly allowance').click();

    // Set amount
    await page.locator('.mud-input-control').filter({ hasText: 'Amount per week' }).locator('input').fill('5.00');

    // Save
    await page.locator('button:has-text("Save Settings")').click();

    // Should show success snackbar
    await expect(page.getByText(/Settings saved/i)).toBeVisible({ timeout: 10000 });
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

  test('should show day of week selector', async ({ page }) => {
    await loginAsParent(page);
    
    // Navigate using dashboard button
    await page.locator('button:has-text("Settings")').click();
    await page.waitForLoadState('networkidle');

    // Check for day of week selector (MudSelect)
    await expect(page.locator('.mud-input-control').filter({ hasText: 'Day of week' })).toBeVisible({ timeout: 10000 });
  });
});
