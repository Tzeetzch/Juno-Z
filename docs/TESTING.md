# Testing Guide

> How to write and run tests for Juno Bank.

## Test Projects

| Project | Type | Location |
|---------|------|----------|
| JunoBank.Tests | xUnit unit tests | `tests/JunoBank.Tests/` |
| E2E | Playwright | `tests/e2e/` |

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
│   └── DatabaseTestBase.cs    # In-memory SQLite setup
├── Services/
│   ├── AllowanceServiceTests.cs
│   ├── AuthServiceTests.cs
│   └── UserServiceTests.cs
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

Tests run against `http://localhost:5209`. App must be running first.

### Test Credentials

```typescript
// helpers.ts
export const PARENT_EMAIL = 'dad@junobank.local';
export const PARENT_PASSWORD = 'parent123';
export const CHILD_PICTURE_SEQUENCE = ['🐱', '🐶', '⭐', '🌙'];  // Junior
export const SOPHIE_PICTURE_SEQUENCE = ['⭐', '🌙', '🐱', '🐶'];  // Sophie
```

### Login Helpers

```typescript
import { loginAsParent, loginAsChild, loginAsChildByName } from './helpers';

test('parent can view dashboard', async ({ page }) => {
  await loginAsParent(page);
  // Now at /parent
});

test('child can view balance', async ({ page }) => {
  await loginAsChild(page);  // Logs in as Junior
  // Now at /child
});

test('Sophie can login', async ({ page }) => {
  await loginAsChildByName(page, 'Sophie', SOPHIE_PICTURE_SEQUENCE);
});
```

### Critical Rule: No page.goto() After Login

**Blazor Server keeps session via SignalR.** Using `page.goto()` after login breaks the session.

```typescript
// ❌ WRONG - breaks session
await loginAsParent(page);
await page.goto('/parent/settings');

// ✅ CORRECT - use in-app navigation
await loginAsParent(page);
await page.locator('a:has-text("Settings")').click();
```

### Test File Structure

```
tests/e2e/
├── specs/
│   ├── smoke.spec.ts              # Quick smoke tests
│   ├── child-dashboard.spec.ts
│   ├── child-requests.spec.ts
│   ├── parent-dashboard.spec.ts
│   ├── parent-requests.spec.ts
│   └── ...
├── helpers.ts                      # Login helpers, constants
├── playwright.config.ts
└── package.json
```

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
