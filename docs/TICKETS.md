# Juno Bank - Issue Tickets

Generated from code reviews on February 2, 2026.

---

## Ticket Status Legend
- ⬜ **Open** - Not started
- 🔄 **In Progress** - Currently being worked on
- ✅ **Done** - Completed
- ⏸️ **Parked** - Deferred/Won't fix

---

## 🔴 HIGH Priority

### TICKET-001: Remove database files from git tracking
**Source:** Implementation Review  
**Effort:** Small (30 min)  
**Status:** ⬜ Open

**Description:**
Database files are being tracked in git, potentially exposing test data and causing merge conflicts.

**Acceptance Criteria:**
- [ ] Update `.gitignore` to exclude `Data/*.db`, `Data/*.db-shm`, `Data/*.db-wal`
- [ ] Add `Data/.gitkeep` to preserve empty folder
- [ ] Remove existing database files from git tracking (not from disk)
- [ ] Commit and verify files are ignored

---

### TICKET-002: Clean up orphaned test database files
**Source:** Architecture Review, Implementation Review  
**Effort:** Small (30 min)  
**Status:** ⬜ Open

**Description:**
60+ orphaned test database files exist in `Data/` folder from E2E test runs.

**Acceptance Criteria:**
- [ ] Delete all `Data/junobank-test-*.db*` files from disk
- [ ] Add `Data/junobank-test-*.db*` pattern to `.gitignore`
- [ ] Consider adding E2E test cleanup in teardown (optional)

---

### TICKET-003: Create first-run setup wizard
**Source:** Implementation Review, Security Review  
**Effort:** Large (8-12 hours)  
**Status:** ⬜ Open

**Description:**
Replace hardcoded demo credentials with a first-run setup wizard that lets users create their own accounts.

**Acceptance Criteria:**
- [ ] Create `/setup` route that activates when database is empty
- [ ] Setup wizard creates: Parent 1, Parent 2 (optional), Child account
- [ ] User sets their own passwords
- [ ] User configures child's picture password
- [ ] Remove demo user creation from `DbInitializer.cs`
- [ ] Update E2E tests to work with setup flow

**Alternative (simpler):**
- [ ] First-login setup: Single form when no users exist

---

### TICKET-004: Create IAuthService - Move auth logic from UI
**Source:** Architecture Review  
**Effort:** Medium (4-6 hours)  
**Status:** ⬜ Open

**Description:**
Razor components inject `AppDbContext` directly for login validation. This violates layered architecture.

**Files to modify:**
- `Components/Pages/Auth/ParentLogin.razor`
- `Components/Pages/Auth/ChildLogin.razor`
- `Components/Pages/Parent/Settings.razor`

**Acceptance Criteria:**
- [ ] Create `Services/IAuthService.cs` interface
- [ ] Create `Services/AuthService.cs` implementation
- [ ] Methods: `ValidateParentLoginAsync(email, password)`, `ValidateChildLoginAsync(sequence)`
- [ ] Remove `AppDbContext` injection from all Razor components
- [ ] Update components to use `IAuthService`
- [ ] Add unit tests for AuthService

---

## 🟡 MEDIUM Priority

### TICKET-005: Add parent login rate limiting
**Source:** Security Review  
**Effort:** Medium (2-3 hours)  
**Status:** ⬜ Open

**Description:**
Parent login has no brute-force protection. Unlimited login attempts are allowed.

**Acceptance Criteria:**
- [ ] Track failed login attempts per email (cache or database)
- [ ] Lock account for 5 minutes after 5 failed attempts
- [ ] Show appropriate error message when locked
- [ ] Add to `IAuthService` (from TICKET-004)
- [ ] Add unit tests

---

### TICKET-006: Create IPasswordService for hashing
**Source:** Architecture Review  
**Effort:** Small (1-2 hours)  
**Status:** ⬜ Open

**Description:**
BCrypt hashing is scattered across multiple files. Should be centralized.

**Files using BCrypt directly:**
- `Components/Pages/Auth/ParentLogin.razor`
- `Components/Pages/Parent/Settings.razor`
- `Data/DbInitializer.cs`

**Acceptance Criteria:**
- [ ] Create `Services/IPasswordService.cs` with `HashPassword()` and `VerifyPassword()`
- [ ] Create `Services/PasswordService.cs` implementation
- [ ] Replace all direct BCrypt usage with service
- [ ] Register in DI

---

### TICKET-007: Consolidate DateTime providers
**Source:** Architecture Review  
**Effort:** Small (1 hour)  
**Status:** ⬜ Open

**Description:**
Two time abstractions exist: custom `IDateTimeProvider` and .NET's `TimeProvider`. Should use one.

**Acceptance Criteria:**
- [ ] Migrate `AllowanceService` to use `TimeProvider` instead of `IDateTimeProvider`
- [ ] Delete `Services/IDateTimeProvider.cs`
- [ ] Update `AllowanceBackgroundService` tests if needed
- [ ] Remove `IDateTimeProvider` from DI registration

---

### TICKET-008: Remove duplicate allowance logic from UserService
**Source:** Architecture Review  
**Effort:** Small (1-2 hours)  
**Status:** ⬜ Open

**Description:**
`UserService` has `UpdateAllowanceSettingsAsync` and `CalculateNextRun` that duplicate `AllowanceService`.

**Acceptance Criteria:**
- [ ] Remove `UpdateAllowanceSettingsAsync` from `UserService`
- [ ] Remove `CalculateNextRun` from `UserService`
- [ ] Remove `GetAllowanceSettingsAsync` from `IUserService` (already in `IAllowanceService`)
- [ ] Update any callers to use `IAllowanceService` directly

---

### TICKET-009: Create SecurityUtils for shared hash functions
**Source:** Code Quality Review  
**Effort:** Small (30 min)  
**Status:** ⬜ Open

**Description:**
SHA256 hashing for picture passwords is duplicated in two files.

**Files:**
- `Data/DbInitializer.cs` - `HashSequence()`
- `Components/Pages/Auth/ChildLogin.razor` - `HashSequence()`

**Acceptance Criteria:**
- [ ] Create `Utils/SecurityUtils.cs`
- [ ] Move `HashPictureSequence(string sequence)` method there
- [ ] Update both files to use shared method

---

### TICKET-010: Delete unused Blazor template files
**Source:** Code Quality Review  
**Effort:** Trivial (5 min)  
**Status:** ⬜ Open

**Description:**
Default Blazor template files are not used.

**Acceptance Criteria:**
- [ ] Delete `Components/Pages/Counter.razor`
- [ ] Delete `Components/Pages/Weather.razor`

---

## 🟢 LOW Priority

### TICKET-011: Create interface for CustomAuthStateProvider
**Source:** Architecture Review  
**Effort:** Small (1-2 hours)  
**Status:** ⬜ Open

**Description:**
`CustomAuthStateProvider` is injected as concrete class, making components harder to test.

**Acceptance Criteria:**
- [ ] Create `IAuthSessionProvider` interface
- [ ] Methods: `GetCurrentUserAsync()`, `LoginAsync()`, `LogoutAsync()`
- [ ] Update components to inject interface
- [ ] Enable mocking in tests

---

### TICKET-012: Create PicturePasswordImages constants
**Source:** Code Quality Review  
**Effort:** Small (30 min)  
**Status:** ⬜ Open

**Description:**
Image names are hardcoded strings in multiple files.

**Acceptance Criteria:**
- [ ] Create `Constants/PicturePasswordImages.cs`
- [ ] Define `static readonly string[] All = { "cat", "dog", ... }`
- [ ] Update `PictureGrid.razor` to use constants
- [ ] Update `DbInitializer.cs` if still using picture password

---

### TICKET-013: Create CurrencyFormatter utility
**Source:** Code Quality Review  
**Effort:** Small (30 min)  
**Status:** ⬜ Open

**Description:**
Currency formatting is inconsistent (some use `CultureInfo.InvariantCulture`, others don't).

**Acceptance Criteria:**
- [ ] Create `Utils/CurrencyFormatter.cs`
- [ ] Method: `FormatEuro(decimal amount, bool showSign = false)`
- [ ] Update components to use formatter

---

### TICKET-014: Set AllowedHosts in production config
**Source:** Security Review  
**Effort:** Small (15 min)  
**Status:** ⬜ Open

**Description:**
`AllowedHosts` is set to `*` which allows any host header.

**Acceptance Criteria:**
- [ ] Create `appsettings.Production.json` (if not exists)
- [ ] Set `AllowedHosts` to actual domain
- [ ] Document in DEPLOYMENT.md

---

### TICKET-015: Extract Settings.razor sub-components
**Source:** Code Quality Review  
**Effort:** Medium (2-3 hours)  
**Status:** ⬜ Open

**Description:**
Settings.razor is 346 lines handling both allowance and password change.

**Acceptance Criteria:**
- [ ] Create `Components/Pages/Parent/AllowanceSettings.razor`
- [ ] Create `Components/Pages/Parent/ChangePassword.razor`
- [ ] Settings.razor composes these components
- [ ] Each component under 150 lines

---

### TICKET-016: Create common CSS classes
**Source:** Code Quality Review  
**Effort:** Medium (2 hours)  
**Status:** ⬜ Open

**Description:**
Heavy use of inline styles. Common patterns should be CSS classes.

**Acceptance Criteria:**
- [ ] Add to `wwwroot/css/app.css` or new file:
  - `.page-container` for common page wrapper
  - `.card-section` for spaced card sections
- [ ] Update components to use classes

---

### TICKET-017: Document single-child assumption
**Source:** Architecture Review  
**Effort:** Small (15 min)  
**Status:** ⬜ Open

**Description:**
Codebase assumes exactly one child. This is fine for MVP but should be documented.

**Acceptance Criteria:**
- [ ] Add note to `docs/ARCHITECTURE.md` under "Known Limitations"
- [ ] Describe what would need to change for multi-child support

---

### TICKET-018: Consider Result types for service methods
**Source:** Architecture Review  
**Effort:** Medium (3-4 hours)  
**Status:** ⏸️ Parked

**Description:**
Services throw exceptions for validation failures. Could use Result types instead.

**Notes:**
Parked for future consideration. Current approach works fine for app scale.

---

### TICKET-019: Add status display helpers
**Source:** Code Quality Review  
**Effort:** Small (30 min)  
**Status:** ⬜ Open

**Description:**
Request status to color/text mapping is inline in components.

**Acceptance Criteria:**
- [ ] Create `Utils/StatusDisplayHelper.cs` or extension methods
- [ ] Centralize `GetStatusText()` and `GetStatusColor()`
- [ ] Update components

---

---

## Summary by Priority

| Priority | Count | Total Effort |
|----------|-------|--------------|
| 🔴 HIGH | 4 | ~15-20 hours |
| 🟡 MEDIUM | 6 | ~8-10 hours |
| 🟢 LOW | 9 | ~8-10 hours |
| ⏸️ PARKED | 1 | - |
| **Total** | **20** | **~31-40 hours** |

---

## Suggested Sprints

### Sprint 1: Repository Cleanup (TICKET-001, 002, 010)
- Effort: ~1 hour
- Quick wins, immediate hygiene impact

### Sprint 2: Architecture Cleanup (TICKET-004, 006, 007, 008, 009)
- Effort: ~8-10 hours
- Improves code structure significantly

### Sprint 3: First-Run Setup (TICKET-003)
- Effort: ~8-12 hours
- Makes app production-ready
- Depends on TICKET-004 (IAuthService)

### Sprint 4: Security Hardening (TICKET-005, 014)
- Effort: ~3-4 hours
- Hardens auth flow

### Backlog: Low Priority Items
- TICKET-011 through TICKET-019

---

*Last updated: February 2, 2026*
