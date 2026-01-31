# Scaffolding Plan

Implementation order for Juno Bank. Each phase builds on the previous.

## Phase A: Project Setup ✅ COMPLETE

- [x] Create Blazor Server project (`dotnet new blazor --interactivity Server`)
- [x] Add NuGet packages (EF Core SQLite, BCrypt, MailKit, MudBlazor)
- [x] Configure MudBlazor with dark theme (orange/purple)
- [x] Create base layout with neumorphic CSS (`wwwroot/css/neumorphic.css`)
- [x] Set up SQLite database connection
- [x] Create initial migration

**Verified:** App runs at https://localhost:5001 with themed page

## Phase B: Database & Entities ✅ COMPLETE

- [x] Create entities: User, Transaction, MoneyRequest, ScheduledAllowance, PicturePassword
- [x] Configure EF Core relationships
- [x] Create and apply migrations
- [x] Seed test data (1 child, 2 parents)

**Verified:** Database created with tables

**Seed data:**
- Dad (dad@junobank.local / parent123)
- Mom (mom@junobank.local / parent123)
- Junior (picture password: cat→dog→star→moon, balance: €10)

## Phase C: Authentication ✅ COMPLETE

- [x] Implement CustomAuthStateProvider
- [x] Parent login page (email/password)
- [x] Picture password component (3x3 emoji grid)
- [x] Child login page (tap 4 pictures in sequence)
- [x] Logout functionality
- [x] Protect routes with [Authorize]

**Verified:** Both login methods work, Home page protected

**Test credentials:**
- Parent: dad@junobank.local / parent123
- Child: cat→dog→star→moon (tap the emojis)

## Phase D: Child Features ✅ COMPLETE

- [x] Child dashboard (balance display, large and visual)
- [x] Transaction history list
- [x] Request withdrawal form
- [x] Request deposit form
- [x] Visual feedback on request submission

**Verified:** Child can view balance, history, and submit requests

## Phase E: Parent Features ✅ COMPLETE

- [x] Parent dashboard (pending requests count, child balance)
- [x] Pending requests list with approve/deny
- [x] Manual transaction form (add/subtract)
- [x] Transaction history (all transactions)
- [x] Settings page (scheduled allowance config)

**Verified:** Parent can manage requests and transactions

## Phase F: Scheduled Allowance ✅ COMPLETE

- [x] BackgroundService for weekly allowance (`AllowanceBackgroundService`)
- [x] Allowance configuration UI (Settings page with description, time picker, day selector)
- [x] `IAllowanceService` with catch-up logic for missed allowances
- [x] `IDateTimeProvider` abstraction for testable time-dependent code
- [x] Unit tests (18 tests in `tests/JunoBank.Tests/`)
- [x] E2E tests (6 new Settings page tests, 53 total)
- [x] Architecture review: approved
- [x] UX review: approved (live preview added)

**Verified:** Allowance auto-deposits on schedule, catches up missed weeks

**Key files:**
- `Services/IAllowanceService.cs` - Interface for allowance operations
- `Services/AllowanceService.cs` - Business logic with catch-up
- `Services/IDateTimeProvider.cs` - Mockable time abstraction
- `BackgroundServices/AllowanceBackgroundService.cs` - Runs every minute (configurable)
- `Components/Pages/Parent/Settings.razor` - Enhanced with description, time picker, live preview

## Phase G: Email Notifications

- [ ] Configure MailKit with SMTP settings
- [ ] Send email on new request
- [ ] Send email on request approval/denial
- [ ] Toggle emails on/off in settings

**Verify:** Emails sent when requests are made/resolved

## Phase H: Docker & Deployment

- [ ] Create Dockerfile
- [ ] Create docker-compose.yml
- [ ] Test local Docker build and run
- [ ] Document reverse proxy setup
- [ ] First production deployment

**Verify:** App runs in Docker, accessible via domain

## Phase I: Polish

- [ ] Responsive design testing (tablet, phone)
- [ ] Neumorphic button styling
- [ ] Kid-friendly icons and colors
- [ ] Error handling and user feedback
- [ ] Final testing with real family use

---

## Current Status

**Completed:** Phase A, Phase B, Phase C, Phase D, Phase E
**Next:** Phase F (Scheduled Allowance) or Phase H (Docker Deployment)

Tell Claude: "Start Phase F" or "Start Phase H" to continue.
