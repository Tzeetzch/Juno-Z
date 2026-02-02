# Project Status

## Current Phase: K (First-Run Setup Wizard)

**Completed:** Phases A through J  

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
| I | Polish (responsive, UX refinements) |
| J | Multi-Child Support |

---

## Future Phases

### Phase K: First-Run Setup Wizard
Replace demo credentials with user-created accounts.  

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

## Test Credentials

- **Parent:** dad@junobank.local / parent123
- **Child (Junior):** Tap cat → dog → star → moon
- **Child (Sophie):** Tap star → moon → cat → dog

---

## Open Tickets

### HIGH

**TICKET-005: Parent login rate limiting** (2-3 hours)  
Lock account for 5 min after 5 failed attempts.

### MEDIUM

**TICKET-017: Paginate all list pages** (3-4 hours)  
Add pagination to transaction history, request history, and standing orders lists.
- Use MudBlazor's built-in pagination component
- Default page size: 20 items
- "Load more" or page numbers approach TBD

**TICKET-015: Extract Settings.razor sub-components** (2-3 hours)  
Split into AllowanceSettings.razor and ChangePassword.razor.

**TICKET-016: Create common CSS classes** (2 hours)  
Add .page-container, .card-section to reduce inline styles.

### LOW

**TICKET-018: Replace emoji icons with proper icons** (1-2 hours)  
Current emoji icons (💰, 📋, ⚙️, etc.) look too "AI-generated". Replace with MudBlazor Material icons or a consistent icon set.

---

## Parked Features

- Request notifications to parents
- Per-parent notification preferences
- Production SMTP configuration
- Multi-family support (device registration flow)
