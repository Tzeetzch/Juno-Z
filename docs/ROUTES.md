# Route Map

> All application routes with auth requirements.

## Public Routes (No Auth)

| Route | Page | Purpose |
|-------|------|---------|
| `/` | Home.razor | Redirects: setup → login → dashboard (based on state/role) |
| `/login` | Login.razor | Main login page (parent button + child picker) |
| `/login/parent` | ParentLogin.razor | Parent email/password form with rate limiting |
| `/forgot-password` | ForgotPassword.razor | Request password reset email |
| `/reset-password/{Token}` | ResetPassword.razor | Set new password with token |
| `/setup` | SetupWizard.razor | First-run setup wizard (uses EmptyLayout) |
| `/Error` | Error.razor | Error display page |

---

## Child Routes (Requires Child Role)

| Route | Page | Purpose |
|-------|------|---------|
| `/child` | Child/Dashboard.razor | Balance display + recent activity |
| `/child/request-deposit` | Child/RequestDeposit.razor | Request money from parent |
| `/child/request-withdrawal` | Child/RequestWithdrawal.razor | Request to spend money |

---

## Parent Routes (Requires Parent Role)

### Main Pages

| Route | Page | Purpose |
|-------|------|---------|
| `/parent` | Parent/Dashboard.razor | All children cards overview |
| `/parent/requests` | Parent/PendingRequests.razor | All pending requests (all children) |
| `/parent/transaction` | Parent/ManualTransaction.razor | Add/remove money (parent balance) |
| `/parent/settings` | Parent/Settings.razor | Global settings, user management (admin) |

### Per-Child Pages

All routes follow pattern: `/parent/child/{ChildId:int}/...`

| Route | Page | Purpose |
|-------|------|---------|
| `/parent/child/{id}` | Child/ChildDetail.razor | Single child management hub |
| `/parent/child/{id}/requests` | Child/ChildPendingRequests.razor | Pending requests for this child |
| `/parent/child/{id}/request-history` | Child/ChildRequestHistory.razor | Past requests (approved/denied) |
| `/parent/child/{id}/transactions` | Child/ChildTransactionHistory.razor | Transaction history for child |
| `/parent/child/{id}/transaction` | Child/ChildManualTransaction.razor | Add/subtract money manually |
| `/parent/child/{id}/settings` | Child/ChildSettings.razor | Standing orders list |
| `/parent/child/{id}/order/new` | Child/ChildOrderEditor.razor | Create new standing order |
| `/parent/child/{id}/order/{orderId}` | Child/ChildOrderEditor.razor | Edit existing standing order |

---

## Route Constants

Always use `AppRoutes` class instead of magic strings:

```csharp
// Good
Navigation.NavigateTo(AppRoutes.Parent.Dashboard);
Navigation.NavigateTo(AppRoutes.Parent.ChildDetail(childId));

// Bad
Navigation.NavigateTo("/parent");
Navigation.NavigateTo($"/parent/child/{childId}");
```

See [API.md](API.md) for full `AppRoutes` reference.

---

## Navigation Flows

### First Run (No Users)
```
/ → /setup → (4 steps) → /parent
```

### Child Login → Dashboard
```
/login → (pick child) → (picture password) → /child
```

### Parent Login → Child Management
```
/login → /login/parent → /parent → /parent/child/{id} → (actions)
```

### Parent Handling Request
```
/parent → (click child card) → /parent/child/{id} → /parent/child/{id}/requests → (approve/deny)
```

### Password Reset
```
/login/parent → /forgot-password → (email) → /reset-password/{token} → /login/parent
```
