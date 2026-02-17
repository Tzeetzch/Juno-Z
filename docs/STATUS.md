# Project Status

## Current Phase: Post-K (feature additions and polish)

**Completed:** Phases A through K

| Phase | Topic | Status |
|-------|-------|--------|
| A | Project Setup (Blazor, MudBlazor, SQLite) | Done |
| B | Database (entities, migrations, seed data) | Done |
| C | Authentication (parent login, picture password) | Done |
| D | Child Features (dashboard, requests) | Done |
| E | Parent Features (approve/deny, transactions) | Done |
| F | Scheduled Allowance (BackgroundService) | Done |
| G | Email Infrastructure (password reset) | Done |
| H | Docker & Deployment | Done |
| I | Polish (responsive, UX refinements) | Done |
| J | Multi-Child Support | Done |
| K | First-Run Setup Wizard | Done |

---

## Phase K: First-Run Setup Wizard

**Status:** Done — UI, service layer, email step, and E2E tests all complete.

**What's done:**
- `ISetupService` with `IsSetupRequiredAsync()`, `HasAdminAsync()`, `CompleteSetupAsync()`
- 4-step wizard UI: Admin → Partner (optional) → Children → Confirmation
- `SetupComplete.razor` success page
- `Home.razor` redirects to `/setup` when no admin exists
- `EmptyLayout.razor` for setup wizard (no nav bar)
- `PictureGridSetup.razor` component for child picture password creation
- `DbInitializer` only seeds demo data when `JUNO_SEED_DEMO=true`

**Spec Summary:**
- 4-step wizard: Parent 1 → Parent 2 (optional) → Children → Confirmation
- Parent fields: name, email, password (min 8 chars), confirm password
- Child fields: name, birthday (no age restriction), starting balance (€0-10000), picture password
- Add multiple children in Step 3
- All users created in single DB transaction
- Progress indicator "Step X of 4"
- Back navigation within session, refresh restarts
- E2E: JUNO_SEED_DEMO=true env flag for test seeding

**Validation:**
- Names: 1-50 chars, trimmed
- Email: valid format, 254 max, case-insensitive uniqueness
- Password: min 8 chars, confirm must match
- Birthday: valid date, strictly before today
- Balance: 0-10000, 2 decimal places
- Picture password: 4 selections from 3x3 grid (repeats allowed)

**UX:**
- Numbers 1-4 shown on selected pictures
- Edit links on confirmation step
- Loading spinner during submit

---

## Parent Login Rate Limiting

**Status:** Done (committed in `e0312d5`)

**What's done:**
- `User` entity: `FailedLoginAttempts`, `LockoutUntil` fields
- `AuthService`: 5 failed attempts → 5-minute lockout for parents
- `ParentLogin.razor`: countdown timer, lockout alert display
- Migration: `AddParentLoginRateLimiting`
- Unit tests: `AuthServiceTests` covers rate limiting scenarios
- E2E spec: `parent-login-ratelimit.spec.ts`

---

## Security Hardening

**Status:** Done

**What's done:**
- **Admin authorization** — `SetAdminStatusAsync`, `CreateParentAsync`, `CreateChildAsync` now require admin role
- **Role validation** — Service-layer `RequireParentAsync` / `RequireAdminAsync` guards with `UnauthorizedAccessException`
- **Cookie hardening** — `HttpOnly`, `SameSite=Strict`, `SecurePolicy=SameAsRequest`, 8-hour sliding expiration
- **Timing-safe comparison** — Picture password uses `CryptographicOperations.FixedTimeEquals`
- **Anti-enumeration** — Dummy BCrypt hash on unknown email login to prevent timing-based email discovery
- **Security headers** — `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`, `X-XSS-Protection`
- **Security audit logging** — Login attempts, lockouts, admin actions, transactions logged via `ILogger`
- **Unit tests** — 3 new authorization tests (`SetAdminStatusAsync_ThrowsWhenCallerNotAdmin`, `CreateParentAsync_ThrowsWhenCallerNotAdmin`, `CreateChildAsync_ThrowsWhenCallerNotAdmin`)

---

## Test Results (2026-02-17)

**All 205 tests passing.**

| Suite | Pass | Fail | Total |
|-------|------|------|-------|
| Unit tests (xUnit) | 135 | 0 | 135 |
| E2E tests (Playwright) | 70 | 0 | 70 |

**E2E spec breakdown:**

| Spec file | Tests | Status |
|-----------|-------|--------|
| smoke.spec.ts | 2 | Pass |
| example.spec.ts | 7 | Pass |
| child-dashboard.spec.ts | 5 | Pass |
| child-requests.spec.ts | 3 | Pass |
| forgot-password.spec.ts | 8 | Pass |
| parent-dashboard.spec.ts | 6 | Pass |
| parent-history.spec.ts | 4 | Pass |
| parent-login-ratelimit.spec.ts | 2 | Pass |
| parent-requests.spec.ts | 6 | Pass |
| parent-settings.spec.ts | 12 | Pass |
| parent-transaction.spec.ts | 5 | Pass |
| request-deposit.spec.ts | 4 | Pass |
| request-withdrawal.spec.ts | 4 | Pass |
| setup-wizard.spec.ts | 2 | Pass |

---

## Test Credentials

- **Parent:** dad@junobank.local / parent123
- **Parent 2:** mom@junobank.local / parent123
- **Child (Junior):** Tap cat → dog → star → moon
- **Child (Sophie):** Tap star → moon → cat → dog

---

## Open Tickets

All previously open tickets have been resolved:

- ~~TICKET-015: Extract Settings.razor sub-components~~ — Done (AdminPanel.razor extracted)
- ~~TICKET-016: Create common CSS classes~~ — Done (.page-container classes added)
- ~~TICKET-017: Paginate all list pages~~ — Done (skip/limit + "Load more" buttons)
- ~~TICKET-018: Replace emoji icons with proper icons~~ — Done (Material Icons for functional UI)

### CODE ISSUES (Resolved)

- ~~AppRoutes.Child.Dashboard mismatch~~ — Fixed: now `/child`
- ~~AppRoutes.Parent.TransactionHistory unused~~ — Removed

---

## Password Recovery (Three-Layer Fix)

**Status:** Done

**What's done:**
- **Layer 1: SMTP via Setup Wizard** — New Step 4 (Email) in setup wizard. Gmail App Password instructions, test email button, writes `email-config.json` to data volume. `Program.cs` loads it as optional config source. Environment variables still override.
- **Layer 2: Admin resets other parent** — `ResetParentPasswordAsync` in UserService (admin-only, clears lockout). Reset Password button in AdminPanel for non-self parents.
- **Layer 3: CLI emergency reset** — `docker exec junobank dotnet JunoBank.Web.dll reset-password user@email.com newpassword` resets password and clears lockout without starting web server.
- **Dashboard UX** — Settings icon added to parent dashboard for navigation.
- **Unit tests** — 7 new tests for `ResetParentPasswordAsync` (admin success, non-admin, self-reset, not found, child target, short password, lockout cleared)
- **E2E test** — Settings icon visibility on dashboard

---

## Shared Component Extraction

**Status:** Done

**What's done:**
- **MoneyInput** — Standardized € currency input replacing 7 inline MudNumericField instances. Fixed Step=0.50m bug in standing orders, $ symbol in admin panel, missing € adornment in setup wizard.
- **ErrorAlert** — Conditional alert with optional close button replacing 12 inline patterns.
- **SubmitButton** — Loading spinner + submit button replacing 7 inline patterns.
- **DescriptionField** — Multi-line textarea (MaxLength=500, Counter) replacing 4 inline patterns.
- **PasswordFields** — Password + Confirm pair with visibility toggle replacing 3 inline patterns.

All components in `Components/Shared/`, documented in `docs/COMPONENTS.md`.

---

## SMTP Email Fix

**Status:** Done (commit `9f591c9`)

**What was fixed:**
- `SmtpEmailService` now uses STARTTLS for port 587 (was incorrectly using full SSL)
- Email service DI registration changed from static (build-time) to runtime factory so setup wizard email config takes effect without restart

---

## Browser Timezone Support

**Status:** Done (commit `7809a9d`)

**What's done:**
- `IBrowserTimeService` / `BrowserTimeService` — scoped service detecting browser timezone via JS interop (`Intl.DateTimeFormat`)
- `MainLayout.razor` initializes timezone on first render, calls `StateHasChanged()` to re-render children
- All displayed UTC timestamps converted to local time via `BrowserTime.ToLocal()`
- `ScheduledAllowance` entity stores `TimeZoneId` — background service calculates next run in user's timezone
- Migration: `AddAllowanceTimeZone` (adds `TimeZoneId` TEXT column with "UTC" default)

---

## Dialog Conversions

**Status:** Done (commit `a1b36e2`)

**What's done:**
- **ManualTransactionDialog** — Deposit/withdrawal form opened from ChildDetail (replaced `ChildManualTransaction.razor` page)
- **OrderEditorDialog** — Standing order create/edit opened from ChildSettings (replaced `ChildOrderEditor.razor` page)
- **ResetPicturePasswordDialog** — Picture password reset opened from ChildSettings (new feature)
- Backend: `UpdatePicturePasswordAsync`, `UnlockChildAsync`, `GetChildLockoutStatusAsync` in UserService
- ChildSettings shows lockout status, unlock button, reset password button
- Dead pages and unused route helpers removed
- 10 new unit tests for picture password service methods

---

## Email Settings Outside Wizard

**Status:** Done (commit `c847c96`)

**What's done:**
- `IEmailConfigService` / `EmailConfigService` — read, write, and test SMTP config
- `EmailSettingsDialog` — MudDialog with SMTP form, test email button, pre-fills from saved config
- `Settings.razor` — admin-only email section with configured/not-configured status + Configure button
- Password never exposed from `GetEmailConfig()`; blank password on save preserves existing
- Refactored `SetupService` and `SetupStep4Email` to use shared `IEmailConfigService`
- 10 new unit tests, 3 new E2E tests

---

## Parked Features

- Request notifications to parents
- Per-parent notification preferences
- Multi-family support (device registration flow)
