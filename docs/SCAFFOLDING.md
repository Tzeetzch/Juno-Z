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

## Phase G: Email Infrastructure ✅ COMPLETE

- [x] Configure MailKit with SMTP settings (appsettings.json)
- [x] Create IEmailService interface (SmtpEmailService, ConsoleEmailService)
- [x] Console logging fallback when SMTP not configured
- [x] Password reset token service (15-min expiry, rate limiting, single-use)
- [x] Forgot password page (/forgot-password)
- [x] Reset password page (/reset-password/{token})
- [x] Change password in Settings page
- [x] Demo account blocking (@junobank.local cannot reset/change password)
- [x] Unit tests (26 new tests, 50 total)
- [x] E2E tests (10 new tests, 63 total)
- [x] Architecture review: approved (9/10)
- [x] UX review: approved (9/10)

**Verified:** Password reset flow works, console fallback in dev

**Key files:**
- `Services/IEmailService.cs` - Email abstraction interface
- `Services/SmtpEmailService.cs` - Production SMTP via MailKit
- `Services/ConsoleEmailService.cs` - Dev/test console logging
- `Services/IPasswordResetService.cs` - Token management interface
- `Services/PasswordResetService.cs` - Token logic with security measures
- `Data/Entities/PasswordResetToken.cs` - Token entity
- `Components/Pages/Auth/ForgotPassword.razor` - Email entry, generic success
- `Components/Pages/Auth/ResetPassword.razor` - Token validation, new password

**Security measures:**
- Tokens expire after 15 minutes
- Single-use tokens (marked used after reset)
- Rate limiting: 3 requests per email per hour
- Old tokens invalidated when new one requested
- Generic success messages (don't reveal if email exists)
- Demo accounts blocked from password operations

**PARKED (Future):** Request notifications to parents
- Send email on new request from child
- Send email on request approval/denial
- Per-parent notification preferences

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

**Completed:** Phase A, Phase B, Phase C, Phase D, Phase E, Phase F, Phase G
**Next:** Phase H (Docker Deployment)

Tell Claude: "Start Phase H" to continue.
