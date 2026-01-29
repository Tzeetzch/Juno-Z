import { test, expect } from '@playwright/test';

/**
 * Example E2E tests for Juno Bank
 * These tests use text-based assertions (no screenshots)
 */

test.describe('Home Page', () => {
  test('should load the home page', async ({ page }) => {
    await page.goto('/');

    // Check page title (text-based check)
    await expect(page).toHaveTitle(/Juno Bank/);
  });

  test('should require authentication', async ({ page }) => {
    await page.goto('/');

    // Should redirect to login or show login UI
    await expect(page).toHaveURL(/login/i);
  });
});

test.describe('Parent Login', () => {
  test('should show parent login form', async ({ page }) => {
    await page.goto('/login/parent');

    // Check for email and password fields (text-based)
    await expect(page.getByLabel(/email/i)).toBeVisible();
    await expect(page.getByLabel(/password/i)).toBeVisible();
  });

  test('should login with valid parent credentials', async ({ page }) => {
    await page.goto('/login/parent');

    // Fill in parent credentials
    await page.getByLabel(/email/i).fill('dad@junobank.local');
    await page.getByLabel(/password/i).fill('parent123');
    await page.locator('button:has-text("Login")').click();

    // Should redirect to parent dashboard
    await expect(page).toHaveURL(/parent/i);

    // Verify logged in state (check for logout button or username)
    //await expect(page.getByText(/dad/i)).toBeVisible();
  });

  test('should reject invalid credentials', async ({ page }) => {
    await page.goto('/login/parent');

    await page.getByLabel(/email/i).fill('wrong@email.com');
    await page.getByLabel(/password/i).fill('wrongpassword');
    await page.locator('button:has-text("Login")').click();

    // Should show error message
    await expect(page.getByText(/invalid/i)).toBeVisible();
    //await expect(page.isVisible('text=invalid'));
  });
});

test.describe('Child Picture Login', () => {
  test('should show picture password grid', async ({ page }) => {
    await page.goto('/login/child');

    // Check for picture grid (9 buttons)
    const pictureButtons = page.locator('.picture-btn');
    await expect(pictureButtons).toHaveCount(9);
  });

  test('should login with correct picture sequence', async ({ page }) => {
    await page.goto('/login/child');

    // The test sequence is: cat→dog→star→moon
    // Note: The grid is randomized, so we need to find the right emojis
    await page.locator('button:has-text("🐱")').click();
    await page.locator('button:has-text("🐶")').click();
    await page.locator('button:has-text("⭐")').click();
    await page.locator('button:has-text("🌙")').click();

    // Should redirect to child dashboard
    await expect(page).toHaveURL(/child/i);

    // Verify logged in state (check for welcome heading)
    await expect(page.getByRole('heading', { name: /Hi Junior/i })).toBeVisible();
  });
});
