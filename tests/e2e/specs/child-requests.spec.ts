import { test, expect } from '@playwright/test';

/**
 * Cycle 5: Visual feedback & pending requests on child dashboard
 */

test.describe('Child Requests & Visual Feedback', () => {

  test('should show pending request on dashboard after withdrawal submit', async ({ page }) => {
    // Login as child
    await page.goto('/login/child');
    await page.locator('button:has-text("🐱")').click();
    await page.locator('button:has-text("🐶")').click();
    await page.locator('button:has-text("⭐")').click();
    await page.locator('button:has-text("🌙")').click();
    await page.waitForURL('/child');

    // Submit a withdrawal request
    await page.getByRole('button', { name: /Request Money/i }).click();
    await page.waitForURL('/child/request-withdrawal');
    await page.getByLabel(/Amount/i).fill('3.00');
    await page.getByLabel(/What do you want it for/i).fill('Test withdrawal for e2e');
    await page.getByRole('button', { name: /Ask Mom or Dad/i }).click();
    await expect(page.getByText(/Request Sent/i)).toBeVisible();

    // Go back to dashboard
    await page.getByRole('button', { name: /Back to My Piggy Bank/i }).click();
    await expect(page).toHaveURL('/child');

    // Should see "My Requests" section with the pending request
    await expect(page.getByText('My Requests')).toBeVisible();
    await expect(page.getByText('Test withdrawal for e2e')).toBeVisible();
    await expect(page.getByText(/Waiting/i)).toBeVisible();
  });

  test('should show pending request on dashboard after deposit submit', async ({ page }) => {
    // Login as child
    await page.goto('/login/child');
    await page.locator('button:has-text("🐱")').click();
    await page.locator('button:has-text("🐶")').click();
    await page.locator('button:has-text("⭐")').click();
    await page.locator('button:has-text("🌙")').click();
    await page.waitForURL('/child');

    // Submit a deposit request
    await page.getByRole('button', { name: /Add Money/i }).click();
    await page.waitForURL('/child/request-deposit');
    await page.getByLabel(/Amount/i).fill('15.00');
    await page.getByLabel(/Where did it come from/i).fill('Birthday money from grandma');
    await page.getByRole('button', { name: /Ask Mom or Dad/i }).click();
    await expect(page.getByText(/Request Sent/i)).toBeVisible();

    // Go back to dashboard
    await page.getByRole('button', { name: /Back to My Piggy Bank/i }).click();
    await expect(page).toHaveURL('/child');

    // Should see the deposit request in "My Requests"
    await expect(page.getByText('Birthday money from grandma')).toBeVisible();
    await expect(page.getByText(/Waiting/i).first()).toBeVisible();
  });

  test('should show snackbar confirmation on request submit', async ({ page }) => {
    // Login as child
    await page.goto('/login/child');
    await page.locator('button:has-text("🐱")').click();
    await page.locator('button:has-text("🐶")').click();
    await page.locator('button:has-text("⭐")').click();
    await page.locator('button:has-text("🌙")').click();
    await page.waitForURL('/child');

    // Submit a request
    await page.getByRole('button', { name: /Request Money/i }).click();
    await page.waitForURL('/child/request-withdrawal');
    await page.getByLabel(/Amount/i).fill('2.00');
    await page.getByLabel(/What do you want it for/i).fill('Snackbar test');
    await page.getByRole('button', { name: /Ask Mom or Dad/i }).click();

    // Should see snackbar message
    await expect(page.getByText(/Mom or Dad will check it soon/i)).toBeVisible();
  });
});
