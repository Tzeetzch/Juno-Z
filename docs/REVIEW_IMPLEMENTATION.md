# Implementation Review

**Reviewer:** Implementation Specialist
**Date:** February 2, 2026
**Scope:** Remove demo data from repo, implement first-run setup wizard

---

## Summary

This review focuses on making the app production-ready by:
1. Removing hardcoded demo/test data from the repository
2. Implementing a first-run setup wizard
3. Cleaning up development artifacts

---

## Current State Issues

### ISSUE 1: Hardcoded Demo Users in DbInitializer

**Files:**
- `Data/DbInitializer.cs`

**Impact:** HIGH
**Category:** Production Readiness

**Problem:**
Demo accounts with known passwords are created on every fresh database:
```csharp
var parent1 = new User
{
    Name = "Dad",
    Email = "dad@junobank.local",
    PasswordHash = HashPassword("parent123"),  // ← Known password
};
```

**Risk:**
- Anyone reading source knows the default passwords
- Users might forget to change them
- Creates false sense of security

**Recommendation:**
Replace with a first-run setup wizard (see Implementation Plan below).

---

### ISSUE 2: Orphaned Test Database Files

**Files:**
- `Data/junobank-test-*.db*` (60+ files)

**Impact:** LOW
**Category:** Repository Hygiene

**Problem:**
E2E tests create test databases that aren't cleaned up, polluting the Data folder.

**Recommendation:**
1. Add to `.gitignore`:
   ```
   Data/junobank-test-*.db*
   Data/*.db-shm
   Data/*.db-wal
   ```
2. Delete existing orphaned files
3. Add cleanup to E2E test teardown (optional)

---

### ISSUE 3: Development Database in Repository

**Files:**
- `Data/junobank.db`
- `Data/junobank-test.db`

**Impact:** MEDIUM
**Category:** Data Leakage

**Problem:**
The actual database file might be committed to git, containing:
- Transaction history
- User data from testing
- Potentially real family data if developed locally

**Recommendation:**
1. Add to `.gitignore`:
   ```
   Data/*.db
   !Data/.gitkeep
   ```
2. Create `Data/.gitkeep` to preserve folder
3. Remove `Data/*.db` from git tracking

---

### ISSUE 4: No First-Run Experience

**Files:**
- `Program.cs` (lines 85-91)

**Impact:** HIGH
**Category:** User Experience

**Problem:**
Currently, the app seeds demo data silently. Users don't:
- Create their own parent accounts
- Set their own passwords
- Configure their child's name
- Set up picture password

**Recommendation:**
Implement a setup wizard (see Implementation Plan below).

---

## Implementation Plan: First-Run Setup Wizard

### Overview

When the database is empty (no users), redirect to `/setup` wizard that:
1. Creates the first parent account
2. (Optionally) Creates second parent account
3. Creates child account with name
4. Sets up picture password for child
5. Sets initial balance (or €0)

### New Files Required

| File | Purpose |
|------|---------|
| `Components/Pages/Setup/Welcome.razor` | Landing page explaining setup |
| `Components/Pages/Setup/ParentSetup.razor` | Create parent 1 (and optional parent 2) |
| `Components/Pages/Setup/ChildSetup.razor` | Create child + picture password |
| `Components/Pages/Setup/Complete.razor` | Summary + first login |
| `Services/ISetupService.cs` | Setup logic interface |
| `Services/SetupService.cs` | Setup logic implementation |

### Flow

```
/setup → /setup/parent → /setup/child → /setup/complete → /login
```

### Middleware/Check

Add to `App.razor` or a new middleware:
```csharp
if (!await SetupService.IsSetupCompleteAsync())
{
    Navigation.NavigateTo("/setup");
}
```

### DbInitializer Changes

Replace current `SeedAsync` with:
```csharp
public static async Task<bool> IsDatabaseInitializedAsync(AppDbContext context)
{
    return await context.Users.AnyAsync();
}

// Remove all hardcoded user creation
```

### Security Considerations

- Setup pages should NOT require authentication
- But setup should only work when database is empty
- After first user created, `/setup` redirects to `/login`
- Consider adding a setup token/flag to prevent race conditions

---

## Implementation Plan: E2E Test Data

### Problem
E2E tests need known test data to run consistently.

### Solution
Keep demo data but ONLY for testing environment:

1. Create `TestDataSeeder.cs` (separate from `DbInitializer`)
2. E2E tests call an API endpoint or use test-specific env var
3. Production gets empty database + setup wizard

### Alternative
Generate test data in E2E `global-setup.ts`:
```typescript
// Create users via API or direct DB access before tests
```

---

## Files to Modify

### .gitignore Additions
```gitignore
# Database files
Data/*.db
Data/*.db-shm
Data/*.db-wal
Data/junobank-test-*.db*

# Keep the Data folder
!Data/.gitkeep
```

### Files to Delete
- `Data/junobank.db` (if committed)
- `Data/junobank-test.db` (if committed)
- All `Data/junobank-test-*.db*` files

### Files to Create
- `Data/.gitkeep`
- Setup wizard components (6 files)
- `Services/ISetupService.cs`
- `Services/SetupService.cs`

### Files to Modify
- `Data/DbInitializer.cs` → Remove demo user creation
- `Program.cs` → Use new setup check
- `Components/App.razor` → Add setup redirect logic
- E2E tests → Update to use test-specific seeding

---

## Effort Estimate

| Task | Effort |
|------|--------|
| Clean up .gitignore and test files | Small (30 min) |
| Create setup wizard pages | Medium (4-6 hours) |
| Create SetupService | Small (1-2 hours) |
| Update DbInitializer | Small (30 min) |
| Update E2E test data seeding | Medium (2-3 hours) |
| **Total** | **8-12 hours** |

---

## Alternative: Simpler Approach

If a full setup wizard is too much effort, consider:

### Option A: Environment-Based Demo Data
```csharp
// DbInitializer.cs
public static async Task SeedAsync(AppDbContext context, bool isDevelopment)
{
    if (!isDevelopment) return; // No seeding in production
    
    // Demo data only for development...
}
```

Pros: Quick to implement
Cons: Production users still need manual database setup

### Option B: First-Login Setup
Instead of a wizard, prompt the first user who visits `/login`:
1. "No accounts found. Create admin account?"
2. Single form: email + password + child name
3. Creates parent + child with default picture password

Pros: Simpler UX, fewer pages
Cons: Less control over setup

---

## Recommended Priority

| Priority | Task | Effort |
|----------|------|--------|
| 1 | Clean .gitignore + delete orphans | Small |
| 2 | Remove DBs from git tracking | Small |
| 3 | Decide on setup approach | Decision |
| 4 | Implement chosen setup approach | Medium-Large |
| 5 | Update E2E test seeding | Medium |

---

## Actionable Recommendations Summary

### Immediate (Do Now):
1. Update `.gitignore` with database exclusions
2. Delete orphaned test database files
3. Remove `junobank.db` from git tracking

### Short-term (Next Sprint):
1. Implement setup wizard OR first-login setup
2. Update E2E tests to handle empty database

### Decision Needed:
- **Full wizard** vs **Simple first-login** approach?
- **Keep demo mode** for development vs **Always use setup**?

---

*End of Implementation Review*
