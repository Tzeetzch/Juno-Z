import { test, expect } from '@playwright/test';
import { loginAsParent } from '../helpers';

/**
 * Parent History pages (per-child): Transaction History + Request History.
 *
 * - Transaction History: /parent/child/{id}/transactions (shows money in/out)
 * - Request History: /parent/child/{id}/request-history (shows approved/denied requests)
 *
 * IMPORTANT: Use in-app navigation instead of page.goto() to preserve session.
 */

// Helper to navigate to child detail
async function navigateToChildDetail(page: any, childName: string) {
  await page.locator(`.child-card:has-text("${childName}")`).click();
  await expect(page).toHaveURL(/\/parent\/child\/\d+/, { timeout: 10000 });
  await page.waitForLoadState('networkidle');
}

test.describe('Parent History (Per-Child)', () => {

  test('should navigate to transaction history from child detail', async ({ page }) => {
    await loginAsParent(page);
    await navigateToChildDetail(page, 'Junior');

    // Click transaction history button
    await page.locator('button:has-text("Transaction History")').click();
    await expect(page).toHaveURL(/\/parent\/child\/\d+\/transactions/, { timeout: 10000 });
  });

  test('should show seed transaction on transaction history page', async ({ page }) => {
    await loginAsParent(page);
    await navigateToChildDetail(page, 'Junior');

    await page.locator('button:has-text("Transaction History")').click();
    await page.waitForLoadState('networkidle');

    // Should show child context
    await expect(page.getByText(/Managing: Junior/i)).toBeVisible({ timeout: 10000 });

    // Should show the seed data transaction "Welcome to Juno Bank"
    await expect(page.getByText(/Welcome to Juno Bank/i)).toBeVisible({ timeout: 10000 });
  });

  test('should navigate to request history from child detail', async ({ page }) => {
    await loginAsParent(page);
    await navigateToChildDetail(page, 'Junior');

    await page.locator('button:has-text("Request History")').click();
    await expect(page).toHaveURL(/\/parent\/child\/\d+\/request-history/, { timeout: 10000 });
    await page.waitForLoadState('networkidle');

    // Should show heading
    await expect(page.getByText('📜 Request History')).toBeVisible({ timeout: 10000 });
  });

  test('should navigate back from transaction history', async ({ page }) => {
    await loginAsParent(page);
    await navigateToChildDetail(page, 'Junior');

    await page.locator('button:has-text("Transaction History")').click();
    await page.waitForLoadState('networkidle');

    // ChildContextHeader back button (text: "Back")
    await page.locator('button:has-text("Back")').click();
    await expect(page).toHaveURL(/\/parent\/child\/\d+$/, { timeout: 10000 });
  });
});
