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
export const PARENT_EMAIL_ALT = 'mom@junobank.local';  // For rate limiting tests
export const PARENT_PASSWORD_ALT = 'parent123';
export const CHILD_PICTURE_SEQUENCE = ['🐱', '🐶', '⭐', '🌙'];
export const SOPHIE_PICTURE_SEQUENCE = ['⭐', '🌙', '🐱', '🐶'];

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
 * Login as child user (Junior) via picture password grid.
 * Handles the 2-step flow: select child → enter picture password.
 * After this, use in-app navigation only (no page.goto).
 */
export async function loginAsChild(page: Page): Promise<void> {
  await loginAsChildByName(page, 'Junior', CHILD_PICTURE_SEQUENCE);
}

/**
 * Login as a specific child by name.
 * @param childName - Name of the child to select (e.g., 'Junior', 'Sophie')
 * @param pictureSequence - Array of emoji strings for the picture password
 */
export async function loginAsChildByName(
  page: Page, 
  childName: string, 
  pictureSequence: string[]
): Promise<void> {
  await page.goto('/login');
  await page.waitForLoadState('networkidle');
  
  // Step 1: Select child from picker (if visible)
  // The child picker shows buttons with child names
  const childButton = page.locator(`.child-selector button:has-text("${childName}")`);
  const pickerVisible = await childButton.isVisible({ timeout: 3000 }).catch(() => false);
  
  if (pickerVisible) {
    await childButton.click();
    await page.waitForLoadState('networkidle');
  }
  
  // Step 2: Enter picture password
  await page.waitForSelector('.picture-grid', { timeout: 10000 });
  
  // Click each picture in sequence
  for (const emoji of pictureSequence) {
    await page.locator(`.picture-btn:has-text("${emoji}")`).click();
    await page.waitForTimeout(100); // Small delay between clicks
  }
  
  await expect(page).toHaveURL(/\/child/, { timeout: 15000 });
  await page.waitForLoadState('networkidle');
}

/**
 * Login as Sophie (second child) for multi-child testing.
 */
export async function loginAsSophie(page: Page): Promise<void> {
  await loginAsChildByName(page, 'Sophie', SOPHIE_PICTURE_SEQUENCE);
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
