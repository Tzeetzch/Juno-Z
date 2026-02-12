# Architecture Decisions

> Why things are built the way they are. Read this before "improving" something.

## DR-001: Blazor Server over WebAssembly

**Context:** Learning project for .NET/Blazor
**Decision:** Use Blazor Server hosting model
**Rationale:**
- Simpler to learn - code stays server-side
- Real-time updates via SignalR work great for 3 users on home network
- No WASM download time, instant startup
- Easier debugging

**Do not:** Migrate to Blazor WebAssembly unless there's a specific reason.

---

## DR-002: SQLite over SQL Server

**Context:** Self-hosted Docker deployment
**Decision:** SQLite with EF Core
**Rationale:**
- Zero configuration, single file backup
- Docker-friendly (no separate container)
- Sufficient for 3-user family app
- Database file in `/data` volume for persistence

**Do not:** Add SQL Server unless scaling requires it.

---

## DR-003: Custom Auth over ASP.NET Identity

**Context:** 3-user app with 2 auth methods
**Decision:** Custom cookie auth + BCrypt
**Rationale:**
- ASP.NET Identity is massive overkill for 3 users
- Picture passwords don't fit Identity's model
- Simple to understand and maintain

**Do not:** Add ASP.NET Identity packages.

---

## DR-004: Picture Password for Children

**Context:** 5-year-old can't type passwords
**Decision:** 4-image sequence from 3x3 grid
**Rationale:**
- Age-appropriate (tap pictures)
- 9 images shown from pool of 12, shuffled each time
- SHA256 hash of sequence stored
- 5 attempts before 5-minute lockout (auto-unlock)

**Do not:** Add keyboard password option for children.

---

## DR-005: No Separate API Layer

**Context:** Blazor Server with direct service injection
**Decision:** Services injected directly into components
**Rationale:**
- No REST/gRPC overhead for single-server app
- Faster development
- All users on same network

**Do not:** Add WebAPI controllers unless external integrations needed.

---

## DR-006: MudBlazor + Neumorphic CSS

**Context:** Kid-friendly UI with dark theme
**Decision:** MudBlazor components + custom neumorphic.css
**Rationale:**
- MudBlazor provides solid component library
- Neumorphic overlay for soft 3D button effects
- Dark theme (#1a1a2e) with orange (#FF6B35) and purple (#9B59B6)

**Do not:** Replace MudBlazor with another library.

---

## DR-007: BackgroundService for Scheduled Tasks

**Context:** Weekly/monthly allowance payments
**Decision:** Built-in .NET BackgroundService
**Rationale:**
- No external dependencies (Hangfire, Quartz)
- Runs every minute, checks for due allowances
- Handles catch-up if server was offline

**Do not:** Add third-party scheduler libraries.

---

## DR-008: Standing Orders Not Allowances

**Context:** Parents wanted multiple recurring payments per child
**Decision:** Renamed internally to "standing orders", one-to-many relationship
**Rationale:**
- More like a real bank (standing orders, not just allowances)
- Child can have: weekly pocket money + monthly grandma gift + etc.
- Each order: amount, interval, day/time, description

**Do not:** Revert to single-allowance-per-child model.

---

## DR-009: Child Picker on Main Login Page

**Context:** Simplify login flow for children
**Decision:** Child selector shown directly on `/login` page
**Rationale:**
- One less navigation step for 5-year-old
- Parent button + child list on same page
- Old `/login/child` route is obsolete but kept for backwards compat

**Do not:** Move child picker back to separate page.

---

## DR-010: Console Email in Development

**Context:** Don't want real emails during development
**Decision:** `ConsoleEmailService` in dev, `SmtpEmailService` in prod
**Rationale:**
- Dev: emails logged to console
- Prod: actual SMTP via MailKit
- Configured via email host check in Program.cs (SMTP if `Email:Host` configured, console otherwise)

**Do not:** Configure real SMTP for development environment.

---

## DR-011: First-Run Setup Wizard

**Context:** App shouldn't ship with hardcoded demo accounts in production
**Decision:** Setup wizard at `/setup` when no admin user exists
**Rationale:**
- 4-step wizard: Admin → Partner (optional) → Children → Confirmation
- Demo data only seeded when `JUNO_SEED_DEMO=true` (for E2E tests)
- `Home.razor` checks `ISetupService.IsSetupRequiredAsync()` and redirects
- Uses `EmptyLayout` (no nav bar during setup)
- All accounts created in single transaction

**Do not:** Remove demo seed capability (needed for E2E tests).

---

## DR-012: Login Rate Limiting

**Context:** Prevent brute-force attacks on parent and child accounts
**Decision:** 5 failed attempts → 5-minute lockout for both parents and children
**Rationale:**
- Parent: `FailedLoginAttempts` and `LockoutUntil` on `User` entity
- Child: `FailedAttempts` and `LockedUntil` on `PicturePassword` entity
- Auto-unlock after timeout (no admin intervention needed)
- Countdown timer displayed on ParentLogin page
- Uses `TimeProvider` for testability

**Do not:** Make lockout duration configurable (5 min is fine for family app).
