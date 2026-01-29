# Scaffolding Plan

Implementation order for Juno Bank. Each phase builds on the previous.

## Phase A: Project Setup

- [ ] Create Blazor Server project (`dotnet new blazorserver`)
- [ ] Add NuGet packages (EF Core SQLite, BCrypt, MailKit, MudBlazor)
- [ ] Configure MudBlazor with dark theme (orange/purple)
- [ ] Create base layout with neumorphic CSS
- [ ] Set up SQLite database connection
- [ ] Create initial migration

**Verify:** App runs, shows MudBlazor themed page

## Phase B: Database & Entities

- [ ] Create entities: User, Transaction, MoneyRequest, ScheduledAllowance, PicturePassword
- [ ] Configure EF Core relationships
- [ ] Create and apply migrations
- [ ] Seed test data (1 child, 2 parents)

**Verify:** Database created with tables, seed data visible

## Phase C: Authentication

- [ ] Implement CustomAuthStateProvider
- [ ] Parent login page (email/password)
- [ ] Picture password component (3x3 grid)
- [ ] Child login page (select user, enter picture sequence)
- [ ] Logout functionality
- [ ] Protect routes with [Authorize]

**Verify:** Both login methods work, routes are protected

## Phase D: Child Features

- [ ] Child dashboard (balance display, large and visual)
- [ ] Transaction history list
- [ ] Request withdrawal form
- [ ] Request deposit form
- [ ] Visual feedback on request submission

**Verify:** Child can view balance, history, and submit requests

## Phase E: Parent Features

- [ ] Parent dashboard (pending requests count, child balance)
- [ ] Pending requests list with approve/deny
- [ ] Manual transaction form (add/subtract)
- [ ] Transaction history (all transactions)
- [ ] Settings page (scheduled allowance config)

**Verify:** Parent can manage requests and transactions

## Phase F: Scheduled Allowance

- [ ] BackgroundService for weekly allowance
- [ ] Allowance configuration UI
- [ ] Test with short interval, then set to weekly

**Verify:** Allowance auto-deposits on schedule

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

## To Start Scaffolding

Tell Claude: "Start Phase A scaffolding"

Claude will create the project structure and verify each step.
