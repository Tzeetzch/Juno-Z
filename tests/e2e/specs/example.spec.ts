import { test, expect } from '@playwright/test';

/**
 * Example E2E tests for Juno Bank
 * These tests use text-based assertions (no screenshots)
 * 
 * MudBlazor-specific notes:
 * - MudTextField: Use locator('.mud-input-control').filter({ hasText: 'Label' }).locator('input')
 * - Buttons: Use getByRole or locator with text
 * - Wait for networkidle after navigation for Blazor Server SignalR
 */

test.describe('Home Page', () => {
  test('should load the home page', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');

    // Check page title (text-based check)
    await expect(page).toHaveTitle(/Juno Bank/);
  });

  test('should require authentication', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');

    // Should redirect to login or show login UI
    await expect(page).toHaveURL(/login/i);
  });
});

test.describe('Parent Login', () => {
  test('should show parent login form', async ({ page }) => {
    await page.goto('/login/parent');
    await page.waitForLoadState('networkidle');

    // Check for email and password fields using MudTextField structure
    // MudBlazor renders labels inside the input control
    await expect(page.locator('.mud-input-control').filter({ hasText: 'Email' })).toBeVisible();
    await expect(page.locator('.mud-input-control').filter({ hasText: 'Password' })).toBeVisible();
  });

  test('should login with valid parent credentials', async ({ page }) => {
    await page.goto('/login/parent');
    await page.waitForLoadState('networkidle');

    // Fill in parent credentials using MudBlazor selectors
    await page.locator('.mud-input-control').filter({ hasText: 'Email' }).locator('input').fill('dad@junobank.local');
    await page.locator('.mud-input-control').filter({ hasText: 'Password' }).locator('input').fill('parent123');
    
    // Click the Login button
    await page.locator('button.neu-btn:has-text("Login")').click();

    // Wait for navigation with extended timeout for Blazor Server
    await expect(page).toHaveURL(/\/parent/, { timeout: 15000 });
    await page.waitForLoadState('networkidle');

    // Verify logged in state - check for welcome message
    await expect(page.getByText(/Welcome, Dad/i)).toBeVisible({ timeout: 10000 });
  });

  test('should reject invalid credentials', async ({ page }) => {
    await page.goto('/login/parent');
    await page.waitForLoadState('networkidle');

    // Fill invalid credentials
    await page.locator('.mud-input-control').filter({ hasText: 'Email' }).locator('input').fill('wrong@email.com');
    await page.locator('.mud-input-control').filter({ hasText: 'Password' }).locator('input').fill('wrongpassword');
    await page.locator('button.neu-btn:has-text("Login")').click();

    // Should show error message (MudAlert with "Invalid email or password")
    await expect(page.locator('.mud-alert').filter({ hasText: /invalid/i })).toBeVisible({ timeout: 10000 });
  });
});

test.describe('Child Picture Login', () => {
  test('should show picture password grid', async ({ page }) => {
    await page.goto('/login/child');
    await page.waitForLoadState('networkidle');

    // Check for picture grid (9 buttons)
    const pictureButtons = page.locator('.picture-btn');
    await expect(pictureButtons).toHaveCount(9);
  });

  test('should login with correct picture sequence', async ({ page }) => {
    await page.goto('/login/child');
    await page.waitForLoadState('networkidle');

    // The test sequence is: cat→dog→star→moon (🐱→🐶→⭐→🌙)
    await page.locator('.picture-btn:has-text("🐱")').click();
    await page.locator('.picture-btn:has-text("🐶")').click();
    await page.locator('.picture-btn:has-text("⭐")').click();
    await page.locator('.picture-btn:has-text("🌙")').click();

    // Should redirect to child dashboard
    await expect(page).toHaveURL(/\/child/, { timeout: 15000 });
    await page.waitForLoadState('networkidle');

    // Verify logged in state (check for welcome heading with child name)
    await expect(page.getByText(/Hi.*!/i)).toBeVisible({ timeout: 10000 });
  });
});
