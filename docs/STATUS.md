# Project Status

## Current Phase: I (Polish)

**Completed:** Phases A through H  

| Phase | Topic |
|-------|-------|
| A | Project Setup (Blazor, MudBlazor, SQLite) |
| B | Database (entities, migrations, seed data) |
| C | Authentication (parent login, picture password) |
| D | Child Features (dashboard, requests) |
| E | Parent Features (approve/deny, transactions) |
| F | Scheduled Allowance (BackgroundService) |
| G | Email Infrastructure (password reset) |
| H | Docker & Deployment |

### Phase I Tasks
- [ ] Responsive design testing (tablet, phone)
- [ ] Neumorphic button styling refinement
- [ ] Kid-friendly icons and colors
- [ ] Error handling and user feedback
- [ ] Final testing with real family use

---

## Test Credentials

- **Parent:** dad@junobank.local / parent123
- **Child:** Tap cat → dog → star → moon

---

## Open Tickets

### HIGH

**TICKET-003: First-run setup wizard** (8-12 hours)  
Replace demo credentials with setup wizard when database is empty.

**TICKET-005: Parent login rate limiting** (2-3 hours)  
Lock account for 5 min after 5 failed attempts.

### MEDIUM

**TICKET-015: Extract Settings.razor sub-components** (2-3 hours)  
Split into AllowanceSettings.razor and ChangePassword.razor.

**TICKET-016: Create common CSS classes** (2 hours)  
Add .page-container, .card-section to reduce inline styles.

**TICKET-017: Document single-child assumption** (15 min)  
Add Known Limitations section to ARCHITECTURE.md.

### PARKED

**TICKET-018: Result types for services**  
Deferred - current exception approach works fine for app scale.

---

## Parked Features

- Request notifications to parents
- Per-parent notification preferences
- Production SMTP configuration
