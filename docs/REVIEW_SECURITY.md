# Security Review

**Reviewer:** Security Specialist
**Date:** February 2, 2026
**Scope:** Authentication, authorization, data protection, and vulnerabilities

---

## Summary

| Category | Score | Notes |
|----------|-------|-------|
| **Authentication** | 7/10 | BCrypt good, some implementation concerns |
| **Authorization** | 8/10 | Role-based, properly enforced |
| **Data Protection** | 7/10 | Basics covered, some gaps |
| **Input Validation** | 7/10 | Present but inconsistent |
| **Secrets Management** | 6/10 | Hardcoded demo credentials |

**Overall Score: 7/10** - Acceptable for family use, needs hardening for broader deployment.

---

## Findings

### ISSUE 1: Demo Credentials in Source Code

**Files:**
- `Data/DbInitializer.cs` (lines 19-36)

**Impact:** HIGH (for production deployment)
**Category:** Secrets Management

**Problem:**
Default demo accounts with known passwords are in the source code:
```csharp
Email = "dad@junobank.local",
PasswordHash = HashPassword("parent123"),
```

While the domain `@junobank.local` is blocked from password reset, anyone reading the repo knows these credentials.

**Risk:**
- If someone deploys without changing passwords, they're exposed
- Password patterns might be reused by users

**Recommendation:**
1. Remove seed data entirely for production (covered in Implementation Review)
2. Or: Generate random passwords on first-run and display once
3. Add prominent warning in deployment docs (already exists, good!)

**Effort:** Covered in Implementation Review

---

### ISSUE 2: Picture Password Uses SHA256 (Not Ideal for Passwords)

**Files:**
- `Data/DbInitializer.cs` (line 72-77)
- `Components/Pages/Auth/ChildLogin.razor` (line 99-103)

**Impact:** LOW
**Category:** Cryptography

**Problem:**
Picture password sequences are hashed with SHA256:
```csharp
using var sha256 = SHA256.Create();
var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(sequence));
```

SHA256 is fast, which is actually undesirable for password hashing (makes brute force easier).

**Mitigating Factors:**
- Lockout after 5 failed attempts
- Only 4 images from 12 (limited combinations = ~500 possible sequences)
- This is intentionally kid-friendly, not high-security

**Recommendation:**
For this use case, **accept as-is**. The lockout mechanism provides sufficient protection. The sequence is meant to be simple for a 5-year-old, not cryptographically strong.

**Document:** Add a note in security docs that picture passwords are convenience features, not high-security.

**Effort:** Small (documentation only)

---

### ISSUE 3: No CSRF Protection Explicitly Configured

**Files:**
- `Program.cs` (line 76): `app.UseAntiforgery();`

**Impact:** LOW
**Category:** Web Security

**Problem:**
Blazor Server apps are generally protected from CSRF because they use SignalR (WebSocket), but the antiforgery middleware is registered without explicit configuration.

**Status:** ✅ ACCEPTABLE
The `UseAntiforgery()` call is present. Blazor Server's interactive mode handles this automatically through the circuit.

**Recommendation:** No change needed.

---

### ISSUE 4: Session Storage for Auth State

**Files:**
- `Auth/CustomAuthStateProvider.cs` (line 11): `ProtectedSessionStorage`

**Impact:** MEDIUM
**Category:** Session Security

**Problem:**
User session is stored in `ProtectedSessionStorage`, which:
- Survives page refresh ✅
- Is encrypted at rest ✅
- Is scoped to browser session ✅

**Potential Issue:**
If Data Protection keys aren't persisted, users get logged out when the server restarts.

**Status:** ✅ ADDRESSED
Docker setup includes `DataProtection__Keys` volume mount for key persistence.

**Recommendation:** Add a note in troubleshooting docs about key persistence.

**Effort:** Small (documentation)

---

### ISSUE 5: No Rate Limiting on Parent Login

**Files:**
- `Components/Pages/Auth/ParentLogin.razor` (HandleLogin method)

**Impact:** MEDIUM
**Category:** Brute Force Protection

**Problem:**
Parent login has no rate limiting or lockout mechanism:
```csharp
private async Task HandleLogin()
{
    // No attempt tracking
    var user = await Db.Users.FirstOrDefaultAsync(...);
}
```

An attacker could brute-force parent passwords with unlimited attempts.

**Mitigating Factors:**
- App is typically deployed on local network
- BCrypt's work factor slows attempts
- Low-value target for most attackers

**Recommendation:**
Add basic lockout similar to child login:
1. Track failed attempts per email in a cache (or database)
2. Lock for 5 minutes after 5 failures
3. Add to `IAuthService` when created

**Effort:** Medium (2-3 hours)

---

### ISSUE 6: No Account Lockout Persistence for Child

**Files:**
- `Components/Pages/Auth/ChildLogin.razor` (line 62-93)
- `Data/Entities/PicturePassword.cs` (lines 18-19)

**Impact:** LOW
**Category:** Brute Force Protection

**Status:** ✅ IMPLEMENTED CORRECTLY
Child lockout is stored in database (`LockedUntil`, `FailedAttempts`) and persists across restarts.

```csharp
if (_picturePassword.LockedUntil.HasValue && _picturePassword.LockedUntil > DateTime.UtcNow)
{
    var remaining = (_picturePassword.LockedUntil.Value - DateTime.UtcNow).Minutes;
    _pictureGrid?.ShowError($"Too many attempts. Try again in {remaining} minutes.");
    return;
}
```

**Recommendation:** No change needed. Good implementation.

---

### ISSUE 7: Password Reset Token Security

**Files:**
- `Services/PasswordResetService.cs`

**Impact:** N/A (WELL IMPLEMENTED)
**Category:** Token Security

**Status:** ✅ GOOD IMPLEMENTATION

Reviewed security measures:
- ✅ 15-minute token expiry
- ✅ Single-use (marked as used after reset)
- ✅ Rate limiting (3 per hour per email)
- ✅ Old tokens invalidated on new request
- ✅ Generic success message (doesn't reveal if email exists)
- ✅ Demo accounts blocked
- ✅ Cryptographically secure token generation (32 random bytes)
- ✅ URL-safe encoding

**Recommendation:** No change needed. Excellent implementation.

---

### ISSUE 8: SQL Injection Protection

**Files:**
- All service files using EF Core

**Impact:** N/A (PROTECTED)
**Category:** Injection

**Status:** ✅ SAFE
All database queries use EF Core LINQ, which parameterizes queries automatically:
```csharp
var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == _email);
```

No raw SQL queries found.

**Recommendation:** No change needed.

---

### ISSUE 9: XSS Protection

**Files:**
- All Razor components

**Impact:** N/A (PROTECTED)
**Category:** Cross-Site Scripting

**Status:** ✅ SAFE
Blazor automatically HTML-encodes all output. No `@Html.Raw()` or `MarkupString` usage found.

**Recommendation:** No change needed.

---

### ISSUE 10: Sensitive Data in Logs

**Files:**
- `Services/SmtpEmailService.cs` (line 61)

**Impact:** LOW
**Category:** Information Leakage

**Problem:**
Email subject is logged:
```csharp
_logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
```

For password reset emails, the subject might contain identifiable information.

**Recommendation:**
Either:
1. Mask email addresses in logs: `dad@j*****.local`
2. Or accept for this small-scale app (family logging isn't a concern)

**Effort:** Small (optional)

---

### ISSUE 11: AllowedHosts Set to Wildcard

**Files:**
- `appsettings.json` (line 11): `"AllowedHosts": "*"`

**Impact:** LOW
**Category:** Host Header Attacks

**Problem:**
Wildcard allows any host header, which could enable:
- Cache poisoning (if using a CDN)
- Host header injection attacks

**Mitigating Factors:**
- Nginx reverse proxy typically sets correct host
- No CDN caching expected

**Recommendation:**
In production, set to actual domain:
```json
"AllowedHosts": "juno.yourdomain.com"
```

**Effort:** Small (config change)

---

### ISSUE 12: Email Credentials in Config

**Files:**
- `appsettings.json` (Email section)

**Impact:** MEDIUM
**Category:** Secrets Management

**Problem:**
Email password would be stored in `appsettings.json` if configured. This file might be committed to git.

**Status:** ✅ PARTIALLY ADDRESSED
- Default values are empty (good)
- Docker uses environment variables (good)
- Email is currently disabled (parked for future)

**Recommendation:**
When enabling email, use environment variables or Docker secrets. Never put credentials in `appsettings.json`.

**Effort:** N/A (already designed correctly)

---

## Positive Observations

1. **BCrypt for password hashing** - Correct algorithm with appropriate work factor
2. **Role-based authorization** - `[Authorize(Roles = "Parent")]` properly enforced
3. **Password reset is well-implemented** - All best practices followed
4. **Child lockout works** - Persisted, 5-minute lockout, 5 attempts
5. **No raw SQL** - All queries use EF Core LINQ
6. **Blazor auto-encodes output** - XSS protected by default
7. **Data Protection keys persistence** - Configured for Docker

---

## Recommended Priority

| Priority | Issue | Effort | Impact |
|----------|-------|--------|--------|
| 1 | #1: Demo credentials | Medium | High |
| 2 | #5: Parent login rate limiting | Medium | Medium |
| 3 | #11: Set AllowedHosts in production | Small | Low |
| 4 | #2: Document picture password security | Small | Low |
| 5 | #10: Consider masking emails in logs | Small | Low |

---

## Actionable Recommendations Summary

### Must Fix for Production:
1. Remove/replace demo credentials (covered in Implementation Review)
2. Add rate limiting to parent login

### Should Fix:
1. Set `AllowedHosts` to actual domain in production config
2. Document that picture passwords are convenience, not high-security

### Nice to Have:
1. Mask email addresses in logs
2. Consider adding CAPTCHA for password reset (future)

---

*End of Security Review*
