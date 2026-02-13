import { test, expect } from "@playwright/test";

test.describe("Forgot Password Flow", () => {
  test("should show forgot password page with email input", async ({ page }) => {
    await page.goto("/forgot-password");
    await page.waitForLoadState("networkidle");

    await expect(page.locator("h1")).toHaveText("Forgot Password");
    await expect(page.getByLabel("Email")).toBeVisible();
    await expect(page.getByText("Send Reset Link")).toBeVisible();
  });

  test("should show validation error for empty email", async ({ page }) => {
    await page.goto("/forgot-password");
    await page.waitForLoadState("networkidle");

    await page.locator("button.neu-btn:has-text('Send Reset Link')").click();
    await page.waitForTimeout(500);

    // The error is displayed as a MudAlert which contains this text
    await expect(page.getByText("Please enter your email address")).toBeVisible({ timeout: 10000 });
  });

  test("should show generic success message for any email", async ({ page }) => {
    await page.goto("/forgot-password");
    await page.waitForLoadState("networkidle");

    await page.getByLabel("Email").fill("nonexistent@example.com");
    await page.locator("button.neu-btn:has-text('Send Reset Link')").click();

    // Should show success even for non-existent email (security)
    await expect(page.getByText("If an account with that email exists")).toBeVisible({ timeout: 10000 });
  });

  test("should show generic success for demo account", async ({ page }) => {
    await page.goto("/forgot-password");
    await page.waitForLoadState("networkidle");

    await page.getByLabel("Email").fill("parent@junobank.local");
    await page.locator("button.neu-btn:has-text('Send Reset Link')").click();

    // Demo accounts should also show generic success (don't reveal demo status)
    await expect(page.getByText("If an account with that email exists")).toBeVisible({ timeout: 10000 });
  });

  test("should navigate back to login", async ({ page }) => {
    await page.goto("/forgot-password");
    await page.waitForLoadState("networkidle");

    await page.getByText("Back to Login").click();

    await expect(page).toHaveURL("/login");
  });

  test("should have forgot password link on parent login", async ({ page }) => {
    await page.goto("/login/parent");
    await page.waitForLoadState("networkidle");

    await expect(page.getByText("Forgot Password?")).toBeVisible();
    await page.getByText("Forgot Password?").click();

    await expect(page).toHaveURL("/forgot-password");
  });
});

test.describe("Reset Password Page", () => {
  test("should show invalid token message for bad token", async ({ page }) => {
    await page.goto("/reset-password/invalid-token-123");
    await page.waitForLoadState("networkidle");
    await page.waitForTimeout(1000); // Wait for Blazor to process token validation

    await expect(page.getByText("invalid or has expired")).toBeVisible({ timeout: 10000 });
    await expect(page.getByText("Request New Link")).toBeVisible();
  });

  test("should navigate to forgot password for new link", async ({ page }) => {
    await page.goto("/reset-password/invalid-token");
    await page.waitForLoadState("networkidle");
    await page.waitForTimeout(500);

    await page.locator("button.neu-btn:has-text('Request New Link')").click();

    await expect(page).toHaveURL("/forgot-password");
  });
});
