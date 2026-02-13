# Project Status

## Current Phase: K (First-Run Setup Wizard)

**Completed:** Phases A through J

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

---

## Phase K: First-Run Setup Wizard

**Status:** In progress - UI and service layer implemented, needs testing and polish.

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

## Test Results (2026-02-13)

**All 170 tests passing.**

| Suite | Pass | Fail | Total |
|-------|------|------|-------|
| Unit tests (xUnit) | 105 | 0 | 105 |
| E2E tests (Playwright) | 65 | 0 | 65 |

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
| parent-settings.spec.ts | 7 | Pass |
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

## Parked Features

- Request notifications to parents
- Per-parent notification preferences
- Production SMTP configuration
- Multi-family support (device registration flow)
