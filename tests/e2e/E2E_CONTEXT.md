# E2E Test Context for Juno Bank

> **Status**: ✅ All 52 tests passing (Jan 2026)

## Critical: Blazor Server Session Handling

**The #1 issue**: Blazor Server maintains session state via SignalR. Using `page.goto()` after login **breaks the session** and redirects to `/`.

### ❌ DON'T do this:
```typescript
await loginAsParent(page);
await page.goto('/parent/history');  // BREAKS SESSION - redirects to /
```

### ✅ DO this instead:
```typescript
await loginAsParent(page);
await page.locator('button:has-text("📜 History")').click();  // In-app navigation
await page.waitForURL('**/parent/history');
```

### Navigation Patterns
- **Forward**: Click dashboard buttons (e.g., `button:has-text("📜 History")`)
- **Back**: Click `page.getByText('← Back to Dashboard')`
- **Only use `page.goto()`**: For initial page load BEFORE login

---

## Test Configuration

**File**: `playwright.config.ts`

```typescript
fullyParallel: false,  // REQUIRED - prevents session conflicts
workers: 1,            // REQUIRED - serial execution
```

**Why**: Parallel tests cause Blazor SignalR session conflicts.

---

## Environment Setup

**File**: `src/JunoBank.Web/Properties/launchSettings.json`

The `http-test` profile must use `Development` environment (not `Testing`):
```json
"ASPNETCORE_ENVIRONMENT": "Development"
```

**Why**: `Testing` environment doesn't load CSS/MudBlazor styles properly.

---

## Test Credentials

| Role   | Login Method | Credentials |
|--------|--------------|-------------|
| Parent | Email/Password | `dad@junobank.local` / `parent123` |
| Child  | Picture Password | 🐱 → 🐶 → ⭐ → 🌙 (in order) |

---

## Selector Best Practices

MudBlazor components often render duplicate text. Use specific selectors:

### ❌ Ambiguous:
```typescript
page.getByText('My Money')           // Matches heading AND history section
page.getByText('Weekly Allowance')   // Matches heading AND switch label
```

### ✅ Specific:
```typescript
page.getByRole('heading', { name: 'My Money', exact: true })
page.getByRole('heading', { name: 'Weekly Allowance' })
page.locator('button:has-text("...")')  // For action buttons
```

---

## Helper Functions

Located in test files, reusable patterns:

```typescript
// Parent login
async function loginAsParent(page: Page) {
  await page.goto('/');
  await page.getByText("I'm a Parent").click();
  await page.fill('input[type="email"]', 'dad@junobank.local');
  await page.fill('input[type="password"]', 'parent123');
  await page.click('button:has-text("Sign In")');
  await page.waitForURL('**/parent/dashboard');
}

// Child login (picture password)
async function loginAsChild(page: Page) {
  await page.goto('/');
  await page.getByText("I'm a Kid").click();
  await page.waitForSelector('.picture-grid');
  for (const emoji of ['🐱', '🐶', '⭐', '🌙']) {
    await page.locator(`.picture-grid button:has-text("${emoji}")`).click();
  }
  await page.waitForURL('**/child/dashboard');
}
```

---

## Test File Overview

| File | Tests | Notes |
|------|-------|-------|
| `smoke.spec.ts` | Basic app load | First test to run |
| `parent-dashboard.spec.ts` | Parent UI | Dashboard elements |
| `parent-transaction.spec.ts` | Add money | Uses in-app nav |
| `parent-history.spec.ts` | Transaction list | Uses in-app nav |
| `parent-requests.spec.ts` | Approve/deny | Uses in-app nav |
| `parent-settings.spec.ts` | Allowance config | Uses in-app nav |
| `child-dashboard.spec.ts` | Child UI | Picture password login |
| `child-requests.spec.ts` | Request money | Child flow |
| `request-deposit.spec.ts` | Deposit request | Child → Parent |
| `request-withdrawal.spec.ts` | Withdrawal request | Child → Parent |

---

## Debugging Tips

1. **Run headed**: `npx playwright test --grep "test name" --headed`
2. **Debug mode**: `npx playwright test --debug`
3. **Check WebServer logs**: Look for EF Core queries in terminal output
4. **Session issues**: If test reaches page but sees wrong content, it's a session/navigation issue

---

## Quick Fixes Checklist

If tests suddenly fail:
- [ ] Is the .NET app running? (`dotnet run` in `src/JunoBank.Web`)
- [ ] Check `launchSettings.json` uses `Development` environment
- [ ] Verify `workers: 1` in playwright.config.ts
- [ ] Look for `page.goto()` after login (remove them)
- [ ] Check for ambiguous selectors (add `exact: true` or use roles)
