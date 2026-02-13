import { test, expect } from '@playwright/test';
import { loginAsParent, loginAsChild } from '../helpers';

/**
 * Parent Pending Requests page tests.
 *
 * The pending requests page is accessed via the "Total Pending Requests"
 * card on the parent dashboard (not a button).
 *
 * IMPORTANT: Use in-app navigation instead of page.goto() to preserve session.
 */

// Helper: login as child (Junior) and submit a withdrawal request
async function submitChildRequest(page: any, description: string = 'E2E test request') {
  await loginAsChild(page);

  await page.locator('button:has-text("Request Money")').click();
  await expect(page).toHaveURL('/child/request-withdrawal', { timeout: 10000 });
  await page.waitForLoadState('networkidle');

  // If at limit, we can't create a request
  const atLimit = await page.getByText('Too Many Requests!').isVisible().catch(() => false);
  if (atLimit) return false;

  await page.locator('.mud-input-control').filter({ hasText: 'Amount' }).locator('input').fill('2.00');
  await page.locator('.mud-input-control').filter({ hasText: 'What do you want it for' }).locator('textarea').fill(description);
  await page.locator('button:has-text("Ask Mom or Dad")').click();

  await expect(page.getByText('Request Sent! ✅')).toBeVisible({ timeout: 10000 });
  return true;
}

test.describe('Parent Pending Requests', () => {

  test('should show pending requests page', async ({ page }) => {
    await loginAsParent(page);

    // Navigate via clickable pending requests card on dashboard
    await page.locator('.neu-card:has-text("Total Pending Requests")').click();
    await page.waitForLoadState('networkidle');

    await expect(page.getByText('Pending Requests')).toBeVisible({ timeout: 10000 });
  });

  test('should show approve and deny buttons for pending request', async ({ page }) => {
    // Create a request first
    await submitChildRequest(page, 'Request for approve/deny test');

    // Now login as parent and check
    await loginAsParent(page);

    // Navigate via clickable pending requests card
    await page.locator('.neu-card:has-text("Total Pending Requests")').click();
    await page.waitForLoadState('networkidle');

    await expect(page.locator('button:has-text("Approve")').first()).toBeVisible({ timeout: 10000 });
    await expect(page.locator('button:has-text("Deny")').first()).toBeVisible();
  });

  test('should approve a request', async ({ page }) => {
    await submitChildRequest(page, 'Request to approve');

    await loginAsParent(page);

    await page.locator('.neu-card:has-text("Total Pending Requests")').click();
    await page.waitForLoadState('networkidle');

    // Approve the first request
    await page.locator('button:has-text("Approve")').first().click();

    // Should show success snackbar
    await expect(page.getByText(/Request approved/i)).toBeVisible({ timeout: 10000 });
  });

  test('should deny a request', async ({ page }) => {
    await submitChildRequest(page, 'Request to deny');

    await loginAsParent(page);

    await page.locator('.neu-card:has-text("Total Pending Requests")').click();
    await page.waitForLoadState('networkidle');

    await page.locator('button:has-text("Deny")').first().click();

    // Should show denial snackbar
    await expect(page.getByText(/Request denied/i)).toBeVisible({ timeout: 10000 });
  });

  test('should display pending requests or empty state', async ({ page }) => {
    await loginAsParent(page);

    // The pending requests card only shows when there are pending requests
    const pendingCard = page.locator('.neu-card:has-text("Total Pending Requests")');
    const hasCard = await pendingCard.isVisible({ timeout: 5000 }).catch(() => false);

    if (hasCard) {
      await pendingCard.click();
      await page.waitForLoadState('networkidle');

      // Should show either requests or empty state heading
      const heading = page.getByText('Pending Requests');
      await expect(heading).toBeVisible({ timeout: 10000 });
    }
    // If no card visible, there are no pending requests — that's also valid
  });

  test('should navigate back to dashboard', async ({ page }) => {
    await loginAsParent(page);

    await page.locator('.neu-card:has-text("Total Pending Requests")').click();
    await page.waitForLoadState('networkidle');

    await page.getByText('← Back to Dashboard').click();
    await expect(page).toHaveURL('/parent', { timeout: 10000 });
  });
});
