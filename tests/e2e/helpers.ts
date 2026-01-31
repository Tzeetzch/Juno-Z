import { Page, expect } from '@playwright/test';

/**
 * Shared E2E Test Helpers for Juno Bank
 * 
 * CRITICAL: After login, NEVER use page.goto() - it breaks Blazor Server session.
 * Always use in-app navigation (clicking buttons/links).
 */

// =============================================================================
// TEST CREDENTIALS
// =============================================================================

export const PARENT_EMAIL = 'dad@junobank.local';
export const PARENT_PASSWORD = 'parent123';
export const CHILD_PICTURE_SEQUENCE = ['🐱', '🐶', '⭐', '🌙'];

// =============================================================================
// LOGIN HELPERS
// =============================================================================

/**
 * Login as parent user via email/password form.
 * After this, use in-app navigation only (no page.goto).
 */
export async function loginAsParent(page: Page): Promise<void> {
  await page.goto('/login/parent');
  await page.waitForLoadState('networkidle');
  
  await page.locator('.mud-input-control').filter({ hasText: 'Email' }).locator('input').fill(PARENT_EMAIL);
  await page.locator('.mud-input-control').filter({ hasText: 'Password' }).locator('input').fill(PARENT_PASSWORD);
  await page.locator('button.neu-btn:has-text("Login")').click();
  
  await expect(page).toHaveURL(/\/parent/, { timeout: 15000 });
  await page.waitForLoadState('networkidle');
}

/**
 * Login as child user via picture password grid.
 * After this, use in-app navigation only (no page.goto).
 */
export async function loginAsChild(page: Page): Promise<void> {
  await page.goto('/login/child');
  await page.waitForLoadState('networkidle');
  
  // Wait for picture grid to load
  await page.waitForSelector('.picture-grid', { timeout: 10000 });
  
  // Click each picture in sequence
  for (const emoji of CHILD_PICTURE_SEQUENCE) {
    await page.locator(`.picture-grid button:has-text("${emoji}")`).click();
    await page.waitForTimeout(100); // Small delay between clicks
  }
  
  await expect(page).toHaveURL(/\/child/, { timeout: 15000 });
  await page.waitForLoadState('networkidle');
}

// =============================================================================
// NAVIGATION HELPERS
// =============================================================================

/**
 * Navigate to a parent sub-page using dashboard buttons.
 * DO NOT use page.goto() after login!
 */
export async function navigateParentTo(page: Page, buttonText: string): Promise<void> {
  await page.locator(`button:has-text("${buttonText}")`).click();
  await page.waitForLoadState('networkidle');
}

/**
 * Go back to dashboard using the back link.
 */
export async function backToDashboard(page: Page): Promise<void> {
  await page.getByText('← Back to Dashboard').click();
  await page.waitForLoadState('networkidle');
}

// =============================================================================
// FORM HELPERS
// =============================================================================

/**
 * Fill a MudBlazor text input by its label.
 */
export async function fillMudInput(page: Page, label: string, value: string): Promise<void> {
  await page.locator('.mud-input-control').filter({ hasText: label }).locator('input').fill(value);
}

/**
 * Fill a MudBlazor textarea by its label.
 */
export async function fillMudTextarea(page: Page, label: string, value: string): Promise<void> {
  await page.locator('.mud-input-control').filter({ hasText: label }).locator('textarea').fill(value);
}

// =============================================================================
// ASSERTION HELPERS
// =============================================================================

/**
 * Wait for a snackbar message to appear.
 */
export async function expectSnackbar(page: Page, textPattern: RegExp): Promise<void> {
  await expect(page.getByText(textPattern)).toBeVisible({ timeout: 10000 });
}
