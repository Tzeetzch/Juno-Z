# Coding Conventions

Standard .NET conventions for Juno Bank.

## Naming

| Type | Convention | Example |
|------|------------|---------|
| Classes | PascalCase | `TransactionService` |
| Interfaces | I + PascalCase | `ITransactionService` |
| Methods | PascalCase | `GetBalance()` |
| Properties | PascalCase | `CurrentBalance` |
| Private fields | _camelCase | `_dbContext` |
| Parameters | camelCase | `userId` |
| Local variables | camelCase | `totalAmount` |
| Constants | PascalCase | `MaxLoginAttempts` |

## File Organization

- One class per file (exceptions: small related classes)
- File name matches class name
- Group by feature, not by type:
  ```
  src/JunoBank.Web/Components/Pages/Child/Dashboard.razor
  src/JunoBank.Application/Services/UserService.cs
  src/JunoBank.Domain/Entities/User.cs
  src/JunoBank.Infrastructure/Data/AppDbContext.cs
  ```
- Project placement: Services → Application, Entities → Domain, DbContext/Email → Infrastructure, Components/UI → Web

## Code Style

```csharp
// Use expression bodies for simple members
public decimal Balance => _balance;

// Use explicit types for clarity, var for obvious types
var user = await _db.Users.FindAsync(id);  // obvious
Transaction transaction = CreateTransaction();  // less obvious

// Null handling
public async Task<User?> GetUserAsync(int id)
{
    return await _db.Users.FindAsync(id);
}

// Early returns over deep nesting
if (user == null)
    return NotFound();

if (!user.IsActive)
    return Forbid();

// actual logic here
```

## Blazor Components

```razor
@* Component order: *@
@page "/child/dashboard"
@attribute [Authorize(Roles = "Child")]
@inject ITransactionService TransactionService

<PageTitle>My Piggy Bank</PageTitle>

@* HTML markup *@

@code {
    // 1. Parameters
    [Parameter] public int UserId { get; set; }

    // 2. Injectables (if not using @inject)

    // 3. Private fields
    private decimal _balance;

    // 4. Lifecycle methods
    protected override async Task OnInitializedAsync() { }

    // 5. Event handlers
    private async Task OnRequestClick() { }

    // 6. Helper methods
    private string FormatCurrency(decimal amount) => amount.ToString("C");
}
```

## Comments

- Prefer self-documenting code over comments
- Add comments for non-obvious business logic
- No "what" comments, only "why" comments:
  ```csharp
  // Bad: Increment counter
  counter++;

  // Good: Reset after 5 failed attempts per security policy
  if (attempts >= MaxLoginAttempts)
      LockAccount();
  ```

## Tests

- Test file: `{ClassName}Tests.cs`
- Test method: `{Method}_{Scenario}_{ExpectedResult}`
  ```csharp
  public async Task GetBalance_ValidUser_ReturnsBalance()
  public async Task GetBalance_InvalidUser_ReturnsNull()
  ```
