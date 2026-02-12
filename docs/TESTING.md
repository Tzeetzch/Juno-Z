# Testing Guide

> How to write and run tests for Juno Bank.

## Test Projects

| Project | Type | Location | Count |
|---------|------|----------|-------|
| JunoBank.Tests | xUnit unit tests | `tests/JunoBank.Tests/` | 96 tests |
| E2E | Playwright | `tests/e2e/` | 64 specs |

---

## Unit Tests (xUnit)

### Running Tests

```bash
cd tests/JunoBank.Tests
dotnet test
```

### Test Structure

```
tests/JunoBank.Tests/
├── Helpers/
│   └── DatabaseTestBase.cs              # In-memory SQLite setup
├── BackgroundServices/
│   └── AllowanceBackgroundServiceTests.cs  # 3 tests
├── Services/
│   ├── AllowanceServiceTests.cs         # 19 tests
│   ├── AuthServiceTests.cs             # 14 tests (includes rate limiting)
│   ├── ConsoleEmailServiceTests.cs      # 3 tests
│   ├── EmailServiceRegistrationTests.cs # 3 tests
│   ├── PasswordResetServiceTests.cs     # 20 tests (19 Fact + 1 Theory)
│   ├── SetupServiceTests.cs            # 11 tests
│   └── UserServiceTests.cs             # 23 tests
└── JunoBank.Tests.csproj
```

### Database Test Base

All service tests inherit from `DatabaseTestBase` for in-memory SQLite:

```csharp
public class MyServiceTests : DatabaseTestBase
{
    private readonly MyService _service;

    public MyServiceTests()
    {
        _service = new MyService(Db, CreateLogger<MyService>());
    }

    [Fact]
    public async Task MyTest()
    {
        // Db is already available with a fresh in-memory database
        // Use Db.Users.Add(), Db.SaveChangesAsync() to set up test data
    }
}
```

### Time Testing

Use `FakeTimeProvider` from `Microsoft.Extensions.Time.Testing` for time-dependent tests:

```csharp
private readonly FakeTimeProvider _timeProvider;

public AllowanceServiceTests()
{
    _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero));
    _service = new AllowanceService(Db, _timeProvider, CreateLogger<AllowanceService>());
}

[Fact]
public async Task AllowanceRunsAtScheduledTime()
{
    // Advance time to trigger allowance
    _timeProvider.Advance(TimeSpan.FromHours(5));
    await _service.ProcessDueAllowancesAsync();
    // Assert...
}
```

### Test Naming Convention

`MethodName_Scenario_ExpectedResult`

```csharp
[Fact]
public void CalculateNextRunDate_Weekly_SameDayAfterTime_ReturnsNextWeek()

[Fact]
public async Task ProcessDueAllowances_AllowanceIsDue_CreatesTransaction()
```

---

## E2E Tests (Playwright)

### Running Tests

```bash
cd tests/e2e
npm install        # First time only
npm test           # Headless, text output only
```

### Configuration

Tests run against `http://localhost:5208`. The Playwright config auto-starts the .NET app with a fresh test database.

### Test Credentials

```typescript
// helpers.ts
export const PARENT_EMAIL = 'dad@junobank.local';
export const PARENT_PASSWORD = 'parent123';
export const PARENT_EMAIL_ALT = 'mom@junobank.local';
export const PARENT_PASSWORD_ALT = 'parent123';
export const CHILD_PICTURE_SEQUENCE = ['🐱', '🐶', '⭐', '🌙'];  // Junior
export const SOPHIE_PICTURE_SEQUENCE = ['⭐', '🌙', '🐱', '🐶'];  // Sophie
```

### Login Helpers

```typescript
import {
  loginAsParent,
  loginAsChild,
  loginAsChildByName,
  loginAsSophie,
  navigateParentTo,
  backToDashboard,
  fillMudInput,
  fillMudTextarea,
  expectSnackbar,
} from './helpers';

test('parent can view dashboard', async ({ page }) => {
  await loginAsParent(page);
  // Now at /parent
});

test('child can view balance', async ({ page }) => {
  await loginAsChild(page);  // Logs in as Junior
  // Now at /child
});

test('Sophie can login', async ({ page }) => {
  await loginAsSophie(page);
});

test('navigate to settings', async ({ page }) => {
  await loginAsParent(page);
  await navigateParentTo(page, 'Settings');
});

test('fill form fields', async ({ page }) => {
  await fillMudInput(page, 'Email', 'test@example.com');
  await fillMudTextarea(page, 'Description', 'Test description');
});

test('check snackbar message', async ({ page }) => {
  await expectSnackbar(page, /Request submitted/);
});
```

### Critical Rule: No page.goto() After Login

**Blazor Server keeps session via SignalR.** Using `page.goto()` after login breaks the session.

```typescript
// WRONG - breaks session
await loginAsParent(page);
await page.goto('/parent/settings');

// CORRECT - use in-app navigation
await loginAsParent(page);
await page.locator('a:has-text("Settings")').click();
```

### Test File Structure

```
tests/e2e/
├── specs/
│   ├── smoke.spec.ts                 # 2 tests - quick smoke
│   ├── child-dashboard.spec.ts       # 5 tests
│   ├── child-requests.spec.ts        # 3 tests
│   ├── request-deposit.spec.ts       # 4 tests
│   ├── request-withdrawal.spec.ts    # 4 tests
│   ├── parent-dashboard.spec.ts      # 6 tests
│   ├── parent-requests.spec.ts       # 6 tests
│   ├── parent-transaction.spec.ts    # 5 tests
│   ├── parent-history.spec.ts        # 4 tests
│   ├── parent-settings.spec.ts       # 8 tests
│   ├── parent-login-ratelimit.spec.ts # 2 tests
│   ├── forgot-password.spec.ts       # 8 tests
│   └── example.spec.ts              # 7 tests
├── global-setup.ts                    # Database cleanup between runs
├── helpers.ts                         # Login helpers, constants, utilities
├── playwright.config.ts
└── package.json
```

### Playwright Config

| Setting | Value |
|---------|-------|
| Base URL | `http://localhost:5208` |
| Timeout | 30 seconds |
| Workers | 1 (serial execution) |
| Retries | 0 local, 2 CI |
| Browser | Chromium only |
| Screenshots | only-on-failure |
| Video | off |
| Trace | on-first-retry |

Tests run serially (single worker) to avoid Blazor Server session conflicts.

### Waiting for Blazor

Blazor components render asynchronously. Use proper waits:

```typescript
// Wait for specific element
await page.waitForSelector('.child-card', { timeout: 10000 });

// Wait for network idle after navigation
await page.waitForLoadState('networkidle');

// Wait for URL change
await expect(page).toHaveURL(/\/parent/, { timeout: 15000 });
```

---

## What to Test

### Unit Tests

- **Services:** Business logic, calculations, database operations
- **Utilities:** Formatters, helpers, hash functions
- **Background services:** Scheduled task processing

### E2E Tests

- **Login flows:** Parent and child authentication
- **Critical paths:** Child requests money → Parent approves → Balance updates
- **Navigation:** Routes work, auth redirects function

### Skip Testing

- MudBlazor component internals
- Basic CRUD without special logic
- CSS styling
