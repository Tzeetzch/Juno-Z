# Juno Bank - E2E Tests

Playwright tests for Juno Bank, configured for **text-based output** to minimize token usage.

## Quick Start

```bash
# Navigate to test directory
cd tests/e2e

# Install dependencies (first time only)
npm install

# Run all tests (headless, auto-starts server)
npm test

# Watch tests run in UI mode (interactive, best for debugging)
npm run test:ui

# See the browser while tests run
npm run test:headed

# Step through tests line-by-line
npm run test:debug

# View the last HTML report
npm run test:report
```

Playwright will automatically start the dev server before tests and stop it after.

## Configuration Philosophy

This setup is optimized to **avoid burning tokens** when Claude analyzes test results:

### What We DO:
✅ Use **text-based assertions** (`expect(page).toHaveTitle(...)`)
✅ Parse **console output** (pass/fail counts, error messages)
✅ Generate **HTML reports** you can open manually
✅ Take screenshots **only on failure**
✅ Use **headless mode** by default

### What We DON'T DO:
❌ Take screenshots for every test
❌ Record videos by default
❌ Have Claude analyze images (very expensive)
❌ Auto-open browser windows

## When Claude Runs Tests

Claude should:
1. Navigate to `tests/e2e` directory
2. Run `npm test` (headless, text output - auto-starts server)
3. Parse the console output for pass/fail
4. Report results to you in text
5. **Only** suggest screenshots if you ask to debug a specific failure

## Writing Tests

Use **text-based locators** and **assertions**:

```typescript
// Good - text-based
await expect(page).toHaveTitle(/Juno Bank/);
await expect(page.getByText(/Balance/)).toBeVisible();
await page.getByRole('button', { name: /Login/ }).click();

// Bad - relies on visual checks
await page.screenshot({ path: 'screenshot.png' });
```

## Directory Structure

```
tests/e2e/
├── specs/
│   ├── example.spec.ts        # Sample tests (auth, login)
│   ├── child-features.spec.ts # Child dashboard, requests (TODO)
│   └── parent-features.spec.ts # Parent dashboard, approvals (TODO)
├── playwright.config.ts       # Playwright configuration
├── package.json               # Node dependencies
├── node_modules/              # Dependencies (gitignored)
└── README.md                  # This file
```

## HTML Reports

After running tests, view the report:

```bash
npm run test:report
```

This opens an HTML report in your browser with:
- Test results
- Failure screenshots (if any)
- Traces for debugging

Claude doesn't need to see this - it's for you to inspect manually.
