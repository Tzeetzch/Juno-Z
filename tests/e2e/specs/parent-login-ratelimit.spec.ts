import { test, expect } from "@playwright/test";
import { PARENT_EMAIL_ALT, PARENT_PASSWORD_ALT, fillMudInput } from "../helpers";

/**
 * NOTE: Uses mom@junobank.local (PARENT_EMAIL_ALT) to avoid locking the main
 * parent account used by other tests.
 * Tests run serially to avoid lockout conflicts.
 */

test.describe.serial("Parent Login Rate Limiting", () => {
  test("should reset counter on successful login", async ({ page }) => {
    await page.goto("/login/parent");
    await page.waitForLoadState("networkidle");

    // Make 2 failed attempts
    await fillMudInput(page, "Email", PARENT_EMAIL_ALT);
    await fillMudInput(page, "Password", "wrong1");
    await page.locator("button.neu-btn:has-text('Login')").click();
    await expect(page.getByText(/4 attempts remaining/)).toBeVisible({ timeout: 10000 });

    await fillMudInput(page, "Password", "wrong2");
    await page.locator("button.neu-btn:has-text('Login')").click();
    await expect(page.getByText(/3 attempts remaining/)).toBeVisible({ timeout: 10000 });

    // Now login successfully
    await fillMudInput(page, "Password", PARENT_PASSWORD_ALT);
    await page.locator("button.neu-btn:has-text('Login')").click();

    // Should redirect to parent dashboard
    await expect(page).toHaveURL(/\/parent/, { timeout: 15000 });

    // Logout via the icon button in the app bar (no text label, just icon)
    await page.locator(".mud-appbar .mud-icon-button").click();
    await page.waitForLoadState("networkidle");

    await page.goto("/login/parent");
    await page.waitForLoadState("networkidle");

    await fillMudInput(page, "Email", PARENT_EMAIL_ALT);
    await fillMudInput(page, "Password", "wrongagain");
    await page.locator("button.neu-btn:has-text('Login')").click();

    // Should be back to 4 attempts (counter was reset)
    await expect(page.getByText(/4 attempts remaining/)).toBeVisible({ timeout: 10000 });
  });

  test("should show attempts remaining and lock after 5 failures with countdown", async ({ page }) => {
    // Reset counter: login successfully first, then logout
    await page.goto("/login/parent");
    await page.waitForLoadState("networkidle");
    await fillMudInput(page, "Email", PARENT_EMAIL_ALT);
    await fillMudInput(page, "Password", PARENT_PASSWORD_ALT);
    await page.locator("button.neu-btn:has-text('Login')").click();
    await expect(page).toHaveURL(/\/parent/, { timeout: 15000 });
    await page.locator(".mud-appbar .mud-icon-button").click();
    await page.waitForLoadState("networkidle");

    await page.goto("/login/parent");
    await page.waitForLoadState("networkidle");

    // Attempt 1
    await fillMudInput(page, "Email", PARENT_EMAIL_ALT);
    await fillMudInput(page, "Password", "wrong1");
    await page.locator("button.neu-btn:has-text('Login')").click();
    await expect(page.getByText(/4 attempts remaining/)).toBeVisible({ timeout: 10000 });

    // Attempt 2
    await fillMudInput(page, "Password", "wrong2");
    await page.locator("button.neu-btn:has-text('Login')").click();
    await expect(page.getByText(/3 attempts remaining/)).toBeVisible({ timeout: 10000 });

    // Attempt 3
    await fillMudInput(page, "Password", "wrong3");
    await page.locator("button.neu-btn:has-text('Login')").click();
    await expect(page.getByText(/2 attempts remaining/)).toBeVisible({ timeout: 10000 });

    // Attempt 4
    await fillMudInput(page, "Password", "wrong4");
    await page.locator("button.neu-btn:has-text('Login')").click();
    await expect(page.getByText(/1 attempts remaining/)).toBeVisible({ timeout: 10000 });

    // Attempt 5 - triggers lockout
    await fillMudInput(page, "Password", "wrong5");
    await page.locator("button.neu-btn:has-text('Login')").click();

    // Should show lockout message with countdown format MM:SS
    await expect(page.getByText(/Account locked\. Try again in/)).toBeVisible({ timeout: 10000 });

    const lockoutText = await page.locator(".mud-alert").textContent();
    expect(lockoutText).toMatch(/Try again in \d+:\d{2}/);

    // Login button should be disabled during lockout
    const loginButton = page.locator("button.neu-btn:has-text('Login')");
    await expect(loginButton).toBeDisabled();

    // Wait a bit and verify countdown updates
    await page.waitForTimeout(2000);
    const updatedLockoutText = await page.locator(".mud-alert").textContent();
    expect(updatedLockoutText).toMatch(/Try again in \d+:\d{2}/);
  });
});
