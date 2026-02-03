# Services & Utilities Reference

> Available services and utility classes for building features.

## Services (`Services/`)

### IAuthService
Handles authentication for parents (email/password) and children (picture password).

```csharp
// Parent login
Task<AuthResult> AuthenticateParentAsync(string email, string password);

// Child login
Task<AuthResult> AuthenticateChildByIdAsync(int childId, string[] pictureSequence);

// Get children for login picker
Task<List<ChildLoginInfo>> GetChildrenForLoginAsync();

// Session management
Task LogoutAsync();
Task<UserSession?> GetCurrentUserAsync();
```

**AuthResult properties:**
- `bool Success` - Whether auth succeeded
- `UserSession? Session` - User session if successful
- `string? Error` - Error message if failed
- `bool IsLockedOut` - Rate limiting active
- `int? AttemptsRemaining` - Tries left before lockout

---

### IUserService
Core service for user data, balances, transactions, and requests.

```csharp
// Balance & Transactions
Task<decimal> GetBalanceAsync(int userId);
Task<List<Transaction>> GetRecentTransactionsAsync(int userId, int limit = 50);
Task<List<Transaction>> GetTransactionsForChildAsync(int childId, int limit = 100);

// Dashboard data
Task<ChildDashboardData> GetChildDashboardDataAsync(int userId);
Task<ParentDashboardData> GetParentDashboardDataAsync();

// Multi-child support
Task<List<ChildSummary>> GetAllChildrenSummaryAsync();
Task<User?> GetChildByIdAsync(int childId);
Task<int> GetOpenRequestCountAsync(int childId);

// Money requests
Task<MoneyRequest> CreateMoneyRequestAsync(int childId, decimal amount, RequestType type, string description);
Task<List<MoneyRequest>> GetPendingRequestsAsync();
Task<List<MoneyRequest>> GetPendingRequestsForChildAsync(int childId);
Task<List<MoneyRequest>> GetCompletedRequestsForChildAsync(int childId, int limit = 50);
Task ResolveRequestAsync(int requestId, int parentUserId, bool approve, string? parentNote = null);

// Manual transactions (parent-initiated)
Task<Transaction> CreateManualTransactionForChildAsync(int parentUserId, int childId, decimal amount, TransactionType type, string description);
```

**Key DTOs:**
- `ChildSummary` - Id, Name, Balance, PendingRequestCount (for parent dashboard cards)
- `ChildDashboardData` - Name, Balance, RecentTransactions, RecentRequests
- `ParentDashboardData` - ChildName, ChildBalance, PendingRequestCount

---

### IAllowanceService
Manages scheduled standing orders (recurring payments to children).

```csharp
// CRUD for standing orders
Task<List<ScheduledAllowance>> GetOrdersForChildAsync(int childId);
Task<ScheduledAllowance?> GetOrderByIdAsync(int orderId);
Task<ScheduledAllowance> CreateOrderAsync(ScheduledAllowance order);
Task UpdateOrderAsync(ScheduledAllowance order);
Task DeleteOrderAsync(int orderId);

// Background processing
Task<int> ProcessDueAllowancesAsync();

// Schedule calculation
DateTime CalculateNextRunDate(ScheduledAllowance allowance, DateTime fromDate);
DateTime CalculateNextRunDate(AllowanceInterval interval, DayOfWeek dayOfWeek, 
    int dayOfMonth, int monthOfYear, TimeOnly timeOfDay, DateTime fromDate);
```

**AllowanceInterval enum:** `Hourly`, `Daily`, `Weekly`, `Monthly`, `Yearly`

---

### IPasswordResetService
Password reset flow for parents.

```csharp
Task<string> GenerateResetTokenAsync(string email);
Task<bool> ValidateTokenAsync(string token);
Task<bool> ResetPasswordAsync(string token, string newPassword);
```

---

### IEmailService
Email sending (Console in dev, SMTP in prod).

```csharp
Task SendEmailAsync(string to, string subject, string body);
```

---

## Utilities (`Utils/`)

### AppRoutes
Centralized URL constants. **Always use these instead of magic strings.**

```csharp
// Child routes
AppRoutes.Child.Dashboard           // "/child/dashboard"
AppRoutes.Child.RequestDeposit      // "/child/request-deposit"
AppRoutes.Child.RequestWithdrawal   // "/child/request-withdrawal"

// Parent routes
AppRoutes.Parent.Dashboard          // "/parent"
AppRoutes.Parent.PendingRequests    // "/parent/requests"
AppRoutes.Parent.Settings           // "/parent/settings"

// Parent child-context routes (methods)
AppRoutes.Parent.ChildDetail(childId)           // "/parent/child/{id}"
AppRoutes.Parent.ChildSettings(childId)         // "/parent/child/{id}/settings"
AppRoutes.Parent.ChildOrderNew(childId)         // "/parent/child/{id}/order/new"
AppRoutes.Parent.ChildOrderEdit(childId, orderId) // "/parent/child/{id}/order/{orderId}"
AppRoutes.Parent.ChildTransactionHistory(childId) // "/parent/child/{id}/transactions"
AppRoutes.Parent.ChildRequestHistory(childId)   // "/parent/child/{id}/request-history"
AppRoutes.Parent.ChildTransaction(childId)      // "/parent/child/{id}/transaction"
AppRoutes.Parent.ChildRequests(childId)         // "/parent/child/{id}/requests"

// Auth routes
AppRoutes.Auth.Login                // "/login"
AppRoutes.Auth.ParentLogin          // "/login/parent"
```

---

### CurrencyFormatter
Consistent Euro formatting.

```csharp
CurrencyFormatter.Format(10.5m)              // "€10.50"
CurrencyFormatter.FormatWithSign(5m, false)  // "+€5.00" (deposit)
CurrencyFormatter.FormatWithSign(5m, true)   // "-€5.00" (withdrawal)
```

---

### StatusDisplayHelper
Request status display helpers.

```csharp
StatusDisplayHelper.GetStatusText(RequestStatus.Pending)   // "⏳ Waiting"
StatusDisplayHelper.GetStatusText(RequestStatus.Approved)  // "✅ Approved"
StatusDisplayHelper.GetStatusText(RequestStatus.Denied)    // "❌ Denied"

StatusDisplayHelper.GetStatusColor(RequestStatus.Pending)  // "#FFA726" (orange)
```

---

### SecurityUtils
Security helpers.

```csharp
SecurityUtils.HashPictureSequence("cat,dog,star,moon")  // SHA256 hash (Base64)
```

---

## Constants (`Constants/`)

### PicturePasswordImages
Picture password configuration.

```csharp
PicturePasswordImages.AllImages           // string[] of 12 image identifiers
PicturePasswordImages.GridDisplayCount    // 9 (3x3 grid)
PicturePasswordImages.DefaultSequenceLength  // 4

PicturePasswordImages.GetEmoji("cat")     // "🐱"
```

---

## Entities (`Data/Entities/`)

### User
```csharp
int Id
string Name
UserRole Role              // Parent, Child
string? Email              // Parent only
string? PasswordHash       // Parent only
PicturePassword? PicturePassword  // Child only
decimal Balance
DateTime CreatedAt
```

### Transaction
```csharp
int Id
int UserId
decimal Amount
TransactionType Type       // Deposit, Withdrawal, Allowance
string Description
bool IsApproved
int? ApprovedByUserId
DateTime CreatedAt
```

### MoneyRequest
```csharp
int Id
int ChildId
decimal Amount
RequestType Type           // Deposit, Withdrawal
string Description
RequestStatus Status       // Pending, Approved, Denied
int? ResolvedByUserId
string? ParentNote
DateTime? ResolvedAt
DateTime CreatedAt
```

### ScheduledAllowance
```csharp
int Id
int ChildId
int CreatedByUserId
decimal Amount
AllowanceInterval Interval // Hourly, Daily, Weekly, Monthly, Yearly
DayOfWeek DayOfWeek       // For Weekly
int DayOfMonth            // For Monthly/Yearly (1-31)
int MonthOfYear           // For Yearly (1-12)
TimeOnly TimeOfDay
string Description
DateTime NextRunDate
DateTime? LastRunDate
bool IsActive
DateTime CreatedAt
```
