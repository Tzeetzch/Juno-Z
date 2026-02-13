# Services & Utilities Reference

> Available services and utility classes for building features.

## Services (`Services/`)

### IAuthService
Handles authentication for parents (email/password) and children (picture password). Includes rate limiting (5 attempts → 5-minute lockout for both).

```csharp
// Parent login
Task<AuthResult> AuthenticateParentAsync(string email, string password);

// Child login (first child found)
Task<AuthResult> AuthenticateChildAsync(string[] pictureSequence);

// Child login (specific child)
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
- `int? LockoutMinutesRemaining` - Minutes until unlock
- `DateTime? LockoutUntil` - Exact lockout expiration
- `int? AttemptsRemaining` - Tries left before lockout

**Static factories:** `Succeeded()`, `Failed()`, `LockedOut()`, `FailedWithAttemptsRemaining()`

---

### IUserService
Core service for user data, balances, transactions, requests, and user management.

```csharp
// Balance & Transactions
Task<decimal> GetBalanceAsync(int userId);
Task<List<Transaction>> GetRecentTransactionsAsync(int userId, int limit = 50);
Task<List<Transaction>> GetTransactionsForChildAsync(int childId, int limit = 100);
Task<List<Transaction>> GetAllTransactionsAsync(int limit = 100);

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

// Manual transactions
Task<Transaction> CreateManualTransactionAsync(int parentUserId, decimal amount, TransactionType type, string description);
Task<Transaction> CreateManualTransactionForChildAsync(int parentUserId, int childId, decimal amount, TransactionType type, string description);

// User management (admin — requires callerUserId with admin role)
Task<List<ParentSummary>> GetAllParentsAsync();
Task<User> CreateParentAsync(string name, string email, string password, bool isAdmin = false, int? callerUserId = null);
Task<User> CreateChildAsync(string name, DateTime birthday, decimal startingBalance, string[] pictureSequence, int createdByUserId, bool requireAdmin = true);
Task SetAdminStatusAsync(int userId, bool isAdmin, int callerUserId);
Task<bool> IsAdminAsync(int userId);
Task ResetParentPasswordAsync(int targetUserId, string newPassword, int callerAdminId);
```

**ResetParentPasswordAsync rules:**
- Caller must be admin
- Cannot reset own password (use forgot-password flow)
- Target must be a parent (not a child)
- Password min 8 chars
- Clears lockout state (FailedLoginAttempts, LockoutUntil)

**Key DTOs:**
- `ChildSummary` - Id, Name, Balance, PendingRequestCount (for parent dashboard cards)
- `ChildDashboardData` - Name, Balance, RecentTransactions, RecentRequests
- `ParentDashboardData` - ChildName, ChildBalance, PendingRequestCount
- `ParentSummary` - Id, Name, Email, IsAdmin

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

### ISetupService
First-run setup wizard — checks if app needs initial configuration and creates accounts.

```csharp
Task<bool> IsSetupRequiredAsync();
Task<bool> HasAdminAsync();
Task<SetupResult> CompleteSetupAsync(SetupData data);
```

**Setup DTOs:**
- `SetupData` - Admin (required), Partner (optional), Children (list), Email (optional)
- `AdminData` - Name, Email, Password
- `PartnerData` - Name, Email, Password
- `ChildData` - Name, Birthday, StartingBalance, PictureSequence
- `EmailConfigData` - Host, Port, Username, Password, FromEmail
- `SetupResult` - Success, Error, AdminUserId

**Email config:** When provided, writes `email-config.json` to the data directory. `Program.cs` loads this as an optional config source. Environment variables override file settings.

---

### IPasswordResetService
Password reset flow for parents.

```csharp
Task<string?> CreateResetTokenAsync(string email);
Task<int?> ValidateTokenAsync(string token);         // Returns userId if valid
Task<bool> ResetPasswordAsync(string token, string newPasswordHash);
bool IsDemoAccount(string email);
```

---

### IPasswordService
BCrypt password hashing abstraction.

```csharp
string HashPassword(string password);
bool VerifyPassword(string password, string hash);
```

---

### IEmailService
Email sending (Console in dev, SMTP in prod).

```csharp
Task<bool> SendEmailAsync(string to, string subject, string htmlBody);
```

---

### IAuthStateProvider
Authentication state management (implemented by CustomAuthStateProvider).

```csharp
Task LoginAsync(UserSession session);
Task LogoutAsync();
Task<UserSession?> GetCurrentUserAsync();
```

---

## Utilities (`Utils/`)

### AppRoutes
Centralized URL constants. **Always use these instead of magic strings.**

```csharp
// Child routes
AppRoutes.Child.Dashboard           // "/child"
AppRoutes.Child.RequestDeposit      // "/child/request-deposit"
AppRoutes.Child.RequestWithdrawal   // "/child/request-withdrawal"

// Parent routes
AppRoutes.Parent.Dashboard          // "/parent"
AppRoutes.Parent.PendingRequests    // "/parent/requests"
AppRoutes.Parent.Settings           // "/parent/settings"

// Parent child-context routes (methods)
AppRoutes.Parent.ChildDetail(childId)             // "/parent/child/{id}"
AppRoutes.Parent.ChildRequests(childId)            // "/parent/child/{id}/requests"
AppRoutes.Parent.ChildRequestHistory(childId)      // "/parent/child/{id}/request-history"
AppRoutes.Parent.ChildTransactionHistory(childId)  // "/parent/child/{id}/transactions"
AppRoutes.Parent.ChildTransaction(childId)         // "/parent/child/{id}/transaction"
AppRoutes.Parent.ChildSettings(childId)            // "/parent/child/{id}/settings"
AppRoutes.Parent.ChildOrderNew(childId)            // "/parent/child/{id}/order/new"
AppRoutes.Parent.ChildOrderEdit(childId, orderId)  // "/parent/child/{id}/order/{orderId}"

// Auth routes
AppRoutes.Auth.Login                // "/login"
AppRoutes.Auth.ParentLogin          // "/login/parent"

// Setup routes
AppRoutes.Setup.Wizard              // "/setup"
```

---

### CurrencyFormatter
Consistent Euro formatting.

```csharp
CurrencyFormatter.Format(10.5m)              // "€10.50"
CurrencyFormatter.FormatWithSign(5m, false)  // "+€5.00" (deposit)
CurrencyFormatter.FormatWithSign(5m, true)   // "-€5.00" (withdrawal)
CurrencyFormatter.FormatInvariant(10.5m)     // Invariant culture format
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
PicturePasswordImages.AllImages               // string[] of 12 image identifiers
PicturePasswordImages.GridDisplayCount        // 9 (3x3 grid)
PicturePasswordImages.DefaultSequenceLength   // 4

PicturePasswordImages.GetEmoji("cat")         // "🐱"
```

**All images:** cat, dog, star, moon, sun, tree, fish, bird, car, flower, heart, apple

---

## Entities (`Data/Entities/`)

### User
```csharp
int Id
string Name
UserRole Role              // Parent, Child
bool IsAdmin               // System admin privilege
string? Email              // Parent only
string? PasswordHash       // Parent only
int? FailedLoginAttempts   // Parent rate limiting
DateTime? LockoutUntil     // Parent lockout expiration
PicturePassword? PicturePassword  // Child only (navigation)
DateTime? Birthday         // Optional (for children)
decimal Balance
DateTime CreatedAt
```

### PicturePassword
```csharp
int Id
int UserId
string ImageSequenceHash   // SHA256 Base64
int GridSize               // Default 9
int SequenceLength         // Default 4
int FailedAttempts         // Child rate limiting
DateTime? LockedUntil      // Child lockout expiration
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

### PasswordResetToken
```csharp
int Id
int UserId
string Token               // Unique, max 100 chars
DateTime ExpiresAt         // 15 minutes from creation
DateTime? UsedAt           // Null if unused
DateTime CreatedAt
```
