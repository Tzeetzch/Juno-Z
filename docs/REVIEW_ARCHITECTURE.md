# Architecture Review

**Reviewer:** Architect
**Date:** February 2, 2026
**Scope:** Full codebase review for clean architecture, SOLID principles, and technical debt

---

## Summary

| Category | Score | Notes |
|----------|-------|-------|
| **Layering** | 7/10 | Layers exist but boundaries are leaky |
| **SOLID Principles** | 7/10 | Good interface usage, some violations |
| **Separation of Concerns** | 6/10 | UI components doing too much |
| **Testability** | 8/10 | Good dependency injection, testable services |
| **Technical Debt** | 6/10 | Several areas need cleanup |

**Overall Score: 7/10** - Solid foundation, but needs refinement before scaling.

---

## Findings

### ISSUE 1: UI Components Directly Access DbContext

**Files:** 
- `Components/Pages/Auth/ParentLogin.razor` (line 64)
- `Components/Pages/Auth/ChildLogin.razor` (line 40)
- `Components/Pages/Parent/Settings.razor` (line 12)

**Impact:** HIGH
**Category:** Separation of Concerns

**Problem:**
Razor components inject `AppDbContext` directly and perform database queries. This violates layered architecture - UI should only talk to services.

```csharp
// ParentLogin.razor
[Inject] private AppDbContext Db { get; set; } = default!;
// ...
var user = await Db.Users.FirstOrDefaultAsync(u => u.Email == _email);
```

**Recommendation:**
1. Create `IAuthService` with `ValidateParentLogin(email, password)` and `ValidateChildLogin(sequence)`
2. Move all authentication logic to service layer
3. Remove `AppDbContext` injection from all Razor components

**Effort:** Medium (4-6 hours)

---

### ISSUE 2: Duplicate Allowance Logic in UserService

**Files:**
- `Services/UserService.cs` (lines 230-287)
- `Services/AllowanceService.cs` (entire file)

**Impact:** MEDIUM
**Category:** DRY Principle

**Problem:**
`UserService` has its own `UpdateAllowanceSettingsAsync` and `CalculateNextRun` methods that duplicate logic in `AllowanceService`. This creates maintenance burden and potential for divergence.

**Recommendation:**
1. Remove allowance methods from `UserService`
2. Have `IUserService.GetAllowanceSettingsAsync` delegate to `IAllowanceService`
3. Or simply use `IAllowanceService` directly from Settings.razor (already done partially)

**Effort:** Small (1-2 hours)

---

### ISSUE 3: BCrypt Used Directly in Components

**Files:**
- `Components/Pages/Auth/ParentLogin.razor` (line 86)
- `Components/Pages/Parent/Settings.razor` (line 308)
- `Data/DbInitializer.cs` (line 69)

**Impact:** MEDIUM
**Category:** Encapsulation

**Problem:**
Password hashing is scattered across multiple files using `BCrypt.Net.BCrypt` directly. This makes it hard to:
- Change hashing algorithm
- Ensure consistent work factor
- Test password validation

**Recommendation:**
Create `IPasswordService` (or add to `IAuthService`):
```csharp
public interface IPasswordService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}
```

**Effort:** Small (1-2 hours)

---

### ISSUE 4: Single Child Assumption Throughout

**Files:**
- `Services/UserService.cs` (lines 55-59, 140-141, 250)
- `Services/AllowanceService.cs` (line 126)

**Impact:** LOW (for current scope)
**Category:** Extensibility

**Problem:**
The codebase assumes there's only one child:
```csharp
var child = await _db.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Child);
```

This works for the MVP family use case but prevents:
- Multiple children
- Multiple families sharing the instance

**Recommendation:**
For now, document this as a known limitation. If multi-child support is needed later:
1. Add `FamilyId` to User
2. Pass `childId` explicitly to all child-related methods
3. Update allowance to support multiple children

**Effort:** Large (if implementing multi-child)

---

### ISSUE 5: Missing Result Types for Operations

**Files:**
- `Services/UserService.cs` (all methods)
- `Services/AllowanceService.cs` (all methods)

**Impact:** MEDIUM
**Category:** Error Handling

**Problem:**
Methods throw exceptions for validation failures:
```csharp
if (amount <= 0)
    throw new ArgumentException("Amount must be greater than zero");
```

Exceptions for expected business rule violations is an anti-pattern. It:
- Makes error handling inconsistent
- Forces try/catch in every caller
- Loses type safety on failure reasons

**Recommendation:**
Create a `Result<T>` type:
```csharp
public record Result<T>(bool Success, T? Value, string? Error);
```

Or use simpler tuple returns:
```csharp
Task<(bool Success, string? Error, Transaction? Transaction)> CreateManualTransactionAsync(...)
```

**Effort:** Medium (3-4 hours to refactor)

---

### ISSUE 6: Two DateTime Providers

**Files:**
- `Services/IDateTimeProvider.cs`
- `Program.cs` (line 43): `TimeProvider.System`

**Impact:** LOW
**Category:** Consistency

**Problem:**
The codebase has two abstractions for time:
- `IDateTimeProvider` (custom, used by `AllowanceService`)
- `TimeProvider` (built-in .NET 8, used by `PasswordResetService`)

**Recommendation:**
Pick one. `TimeProvider` is the .NET standard, so:
1. Migrate `AllowanceService` to use `TimeProvider`
2. Remove `IDateTimeProvider` and `SystemDateTimeProvider`

**Effort:** Small (1 hour)

---

### ISSUE 7: Leftover Test Database Files in Data Folder

**Files:**
- `Data/junobank-test-*.db*` (dozens of orphaned files)

**Impact:** LOW
**Category:** Hygiene

**Problem:**
The `Data/` folder contains ~60+ orphaned test database files that aren't being cleaned up after E2E tests.

**Recommendation:**
1. Add cleanup in E2E test teardown
2. Add to `.gitignore`: `*.db-shm`, `*.db-wal`, `junobank-test-*.db`
3. Delete existing orphans

**Effort:** Small (30 minutes)

---

### ISSUE 8: No Explicit Data Layer Abstraction (Repository Pattern)

**Files:**
- `Services/UserService.cs` (uses `_db` directly)

**Impact:** LOW
**Category:** Architecture Pattern

**Problem:**
Services access `AppDbContext` directly rather than through repositories. This is acceptable for a small app but limits:
- Unit testing (requires mocking DbContext)
- Swapping data stores
- Query reuse

**Recommendation:**
For this app size, **keep it as-is**. The repository pattern would add complexity without proportional benefit. Document this as a deliberate decision.

**Effort:** N/A (no change recommended)

---

### ISSUE 9: Missing Interface for CustomAuthStateProvider

**Files:**
- `Auth/CustomAuthStateProvider.cs`
- All Razor components that inject it

**Impact:** LOW
**Category:** Testability

**Problem:**
`CustomAuthStateProvider` is injected as a concrete class:
```csharp
@inject CustomAuthStateProvider AuthProvider
```

This makes components harder to test because you can't mock the auth provider.

**Recommendation:**
Create `IAuthSessionProvider` interface with:
- `Task<UserSession?> GetCurrentUserAsync()`
- `Task LoginAsync(UserSession session)`
- `Task LogoutAsync()`

**Effort:** Small (1-2 hours)

---

## Positive Observations

1. **Good use of dependency injection** - Services are properly registered and injected
2. **Interface abstractions exist** - `IUserService`, `IAllowanceService`, `IEmailService`, etc.
3. **Testable design for core services** - `AllowanceService` uses `IDateTimeProvider` for time control
4. **Background service is well-designed** - Delegates to service, handles errors gracefully
5. **Entity relationships are clean** - EF Core configuration is explicit and well-organized
6. **Console fallback for email** - Good pattern for dev/test environments

---

## Recommended Priority

| Priority | Issue | Effort | Impact |
|----------|-------|--------|--------|
| 1 | #1: UI accessing DbContext | Medium | High |
| 2 | #3: Centralize password hashing | Small | Medium |
| 3 | #6: Consolidate DateTime providers | Small | Low |
| 4 | #7: Clean up test database files | Small | Low |
| 5 | #2: Remove duplicate allowance logic | Small | Medium |
| 6 | #9: Interface for AuthStateProvider | Small | Low |
| 7 | #5: Result types for operations | Medium | Medium |
| 8 | #4: Document single-child limitation | Small | Low |

---

## Actionable Recommendations Summary

### Quick Wins (< 2 hours each):
1. Consolidate to `TimeProvider` (remove `IDateTimeProvider`)
2. Add `.gitignore` entries and clean test databases
3. Remove duplicate allowance methods from `UserService`

### Medium Priority (2-6 hours):
1. Create `IAuthService` and move auth logic from Razor to service
2. Create `IPasswordService` for hashing operations
3. Add interface for `CustomAuthStateProvider`

### Backlog (consider for future):
1. Implement `Result<T>` pattern for service methods
2. Document single-child assumption in ARCHITECTURE.md

---

*End of Architecture Review*
