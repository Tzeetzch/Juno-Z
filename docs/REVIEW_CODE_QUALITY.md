# Code Quality Review

**Reviewer:** Code Quality Specialist
**Date:** February 2, 2026
**Scope:** Duplication, naming, maintainability, code smells

---

## Summary

| Category | Score | Notes |
|----------|-------|-------|
| **Code Duplication** | 7/10 | Some duplicated patterns |
| **Naming Conventions** | 8/10 | Generally good, minor inconsistencies |
| **Maintainability** | 7/10 | Some long methods, tight coupling |
| **Code Smells** | 7/10 | A few detected |
| **Documentation** | 8/10 | Good XML docs on interfaces |

**Overall Score: 7.5/10** - Clean and readable, with minor improvements needed.

---

## Findings

### ISSUE 1: Duplicated Hash Functions

**Files:**
- `Data/DbInitializer.cs` (lines 69-77)
- `Components/Pages/Auth/ChildLogin.razor` (lines 99-103)

**Impact:** MEDIUM
**Category:** DRY Principle

**Problem:**
The SHA256 hashing for picture passwords is duplicated:

```csharp
// DbInitializer.cs
private static string HashSequence(string sequence)
{
    using var sha256 = SHA256.Create();
    var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(sequence));
    return Convert.ToBase64String(bytes);
}

// ChildLogin.razor (identical)
private static string HashSequence(string sequence)
{
    using var sha256 = SHA256.Create();
    var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(sequence));
    return Convert.ToBase64String(bytes);
}
```

**Recommendation:**
Move to a shared utility class:
```csharp
public static class SecurityUtils
{
    public static string HashPictureSequence(string sequence) { ... }
}
```

**Effort:** Small (30 minutes)

---

### ISSUE 2: Magic Strings for Image Names

**Files:**
- `Components/Shared/PictureGrid.razor` (lines 37-39)
- `Data/DbInitializer.cs` (line 47)

**Impact:** LOW
**Category:** Maintainability

**Problem:**
Image names are hardcoded strings scattered across files:
```csharp
// PictureGrid.razor
private static readonly string[] AllImages = { "cat", "dog", "star", "moon", ... };

// DbInitializer.cs
ImageSequenceHash = HashSequence("cat,dog,star,moon"),
```

If image names change, multiple files need updating.

**Recommendation:**
Create a shared constants class:
```csharp
public static class PicturePasswordImages
{
    public static readonly string[] All = { "cat", "dog", ... };
    public static readonly string DefaultSequence = "cat,dog,star,moon";
}
```

**Effort:** Small (30 minutes)

---

### ISSUE 3: Long Method - UserService.ResolveRequestAsync

**Files:**
- `Services/UserService.cs` (lines 77-124)

**Impact:** LOW
**Category:** Code Complexity

**Problem:**
`ResolveRequestAsync` is 47 lines with multiple responsibilities:
1. Fetch request
2. Validate status
3. Validate parent role
4. Update request status
5. Update child balance (conditionally)
6. Create transaction record
7. Save changes

**Recommendation:**
Extract helper methods:
```csharp
private async Task ValidateRequestCanBeResolved(MoneyRequest request) { ... }
private Transaction CreateTransactionFromRequest(MoneyRequest request, int parentId) { ... }
```

**Effort:** Small (1 hour)

---

### ISSUE 4: Inconsistent Culture Handling

**Files:**
- `Components/Pages/Child/Dashboard.razor` (line 40)
- `Components/Pages/Parent/Dashboard.razor` (lines 28, 29)
- `Components/Shared/TransactionList.razor` (line 61)

**Impact:** LOW
**Category:** Consistency

**Problem:**
Some places use `CultureInfo.InvariantCulture`, others don't:
```csharp
// With culture
€@_balance.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)

// Without (in TransactionList)
return $"{sign}€{amount:F2}";
```

**Recommendation:**
Create a formatting helper:
```csharp
public static class CurrencyFormatter
{
    public static string FormatEuro(decimal amount, bool showSign = false) { ... }
}
```

**Effort:** Small (30 minutes)

---

### ISSUE 5: Repetitive Status Color/Text Logic

**Files:**
- `Components/Pages/Child/Dashboard.razor` (lines 119-134)
- Similar patterns likely in other files

**Impact:** LOW
**Category:** DRY Principle

**Problem:**
Status to color/text mapping is inline in components:
```csharp
private static string GetStatusText(RequestStatus status) => status switch
{
    RequestStatus.Pending => "⏳ Waiting",
    RequestStatus.Approved => "✅ Approved",
    RequestStatus.Denied => "❌ Denied",
    _ => ""
};

private static string GetStatusColor(RequestStatus status) => status switch
{
    RequestStatus.Pending => "#FFA726",
    ...
};
```

**Recommendation:**
Create extension methods or a shared component for status display.

**Effort:** Small (30 minutes)

---

### ISSUE 6: Inconsistent Error Handling in UI

**Files:**
- `Components/Pages/Parent/ManualTransaction.razor` (lines 116-127)
- `Components/Pages/Parent/Settings.razor` (lines 252-266)

**Impact:** LOW
**Category:** Consistency

**Problem:**
Some pages use `_errorMessage` field, others use different approaches:
```csharp
// ManualTransaction
catch (InvalidOperationException ex) { _errorMessage = ex.Message; }
catch (ArgumentException ex) { _errorMessage = ex.Message; }
catch (Exception) { _errorMessage = "Something went wrong. Please try again."; }

// Settings - change password
catch (Exception) { _passwordError = "An error occurred. Please try again."; }
```

**Recommendation:**
Standardize error handling:
1. Define error display pattern
2. Consider a base component or shared error state
3. Or at least use consistent field names

**Effort:** Small (1 hour)

---

### ISSUE 7: Unused Code - Counter.razor and Weather.razor

**Files:**
- `Components/Pages/Counter.razor`
- `Components/Pages/Weather.razor`

**Impact:** LOW
**Category:** Dead Code

**Problem:**
These are default Blazor template files that aren't used in the app.

**Recommendation:**
Delete unused files:
```
Components/Pages/Counter.razor
Components/Pages/Weather.razor
```

**Effort:** Trivial (2 minutes)

---

### ISSUE 8: Nullable Warnings Suppressed

**Files:**
- Various entity classes using `= null!`

**Impact:** LOW
**Category:** Code Quality

**Example:**
```csharp
public User User { get; set; } = null!;
```

**Status:** ✅ ACCEPTABLE
This is the standard EF Core pattern for navigation properties. EF ensures they're loaded when needed.

**Recommendation:** No change needed.

---

### ISSUE 9: Large Razor Components

**Files:**
- `Components/Pages/Parent/Settings.razor` (346 lines)
- `Components/Pages/Child/Dashboard.razor` (135 lines)

**Impact:** LOW
**Category:** Component Size

**Problem:**
Settings.razor handles both allowance configuration AND password change. This violates Single Responsibility.

**Recommendation:**
Extract sub-components:
- `AllowanceSettings.razor`
- `ChangePassword.razor`

Then Settings.razor just composes them.

**Effort:** Medium (2-3 hours)

---

### ISSUE 10: Inline Styles vs CSS Classes

**Files:**
- Most Razor components

**Impact:** LOW
**Category:** Maintainability

**Problem:**
Heavy use of inline styles:
```html
<div style="max-width: 600px; margin: 0 auto; padding: 2rem;">
```

**Mitigating Factors:**
- Consistent pattern throughout
- MudBlazor provides base styling
- Neumorphic CSS handles the custom look

**Recommendation:**
Consider adding CSS classes for common patterns like:
```css
.page-container { max-width: 600px; margin: 0 auto; padding: 2rem; }
```

**Effort:** Medium (2 hours)

---

## Positive Observations

1. **Good interface documentation** - XML comments on interface methods
2. **Consistent file organization** - Clear folder structure
3. **Descriptive method names** - `CreateManualTransactionAsync`, `ResolveRequestAsync`
4. **Appropriate use of async/await** - No blocking calls
5. **Clean entity definitions** - Simple, focused classes
6. **Good use of nullable reference types** - Explicit nullability

---

## Recommended Priority

| Priority | Issue | Effort | Impact |
|----------|-------|--------|--------|
| 1 | #7: Delete unused template files | Trivial | Low |
| 2 | #1: Consolidate hash functions | Small | Medium |
| 3 | #2: Centralize image name constants | Small | Low |
| 4 | #4: Create currency formatter | Small | Low |
| 5 | #5: Standardize status display | Small | Low |
| 6 | #9: Extract Settings sub-components | Medium | Low |
| 7 | #10: CSS classes for common layouts | Medium | Low |

---

## Actionable Recommendations Summary

### Quick Wins (< 30 minutes each):
1. Delete `Counter.razor` and `Weather.razor`
2. Create `SecurityUtils` class with `HashPictureSequence`
3. Create `PicturePasswordImages` constants

### Medium Priority:
1. Create `CurrencyFormatter` utility
2. Standardize status color/text helpers
3. Consider extracting Settings sub-components

### Backlog:
1. Extract common CSS classes
2. Refactor long methods into smaller pieces

---

*End of Code Quality Review*
