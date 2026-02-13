# Components Reference

> Reusable Blazor components available in the project.

## Shared Components (`Components/Shared/`)

### ChildCard.razor
Displays a child summary card on the parent dashboard. Shows avatar (first letter), name, balance, and pending request badge.

**Parameters:**
```csharp
[Parameter, EditorRequired] public ChildSummary Child { get; set; }
[Parameter] public EventCallback<ChildSummary> OnClick { get; set; }
```

**Usage:**
```razor
<ChildCard Child="@childSummary" OnClick="@HandleChildClick" />
```

---

### ChildContextHeader.razor
Header component for child-specific pages. Shows avatar, "Managing: [ChildName]" with back button.

**Parameters:**
```csharp
[Parameter, EditorRequired] public string ChildName { get; set; }
[Parameter] public string BackUrl { get; set; } = AppRoutes.Parent.Dashboard;
```

**Usage:**
```razor
<ChildContextHeader ChildName="@_child.Name" BackUrl="@AppRoutes.Parent.ChildDetail(_childId)" />
```

---

### ChildSelector.razor
Displays a list of children as buttons for selection (used on login page).

**Parameters:**
```csharp
[Parameter] public List<ChildLoginInfo> Children { get; set; }
[Parameter] public EventCallback<ChildLoginInfo> OnChildSelected { get; set; }
```

**Usage:**
```razor
<ChildSelector Children="_children" OnChildSelected="HandleChildSelected" />
```

---

### PictureGrid.razor
Picture password input grid for authentication. Shuffles 9 images from the pool of 12. Child taps 4 to authenticate.

**Parameters:**
```csharp
[Parameter] public int RequiredLength { get; set; } = 4;
[Parameter] public EventCallback<string[]> OnSequenceComplete { get; set; }
[Parameter] public EventCallback OnSequenceReset { get; set; }
```

**Public Methods:**
```csharp
void Reset()                    // Clear selection and reshuffle
void ShowError(string message)  // Display error and clear selection
```

**Usage:**
```razor
<PictureGrid @ref="_pictureGrid" RequiredLength="4" OnSequenceComplete="HandleSequenceComplete" />

@code {
    private PictureGrid? _pictureGrid;

    private async Task HandleSequenceComplete(string[] sequence)
    {
        var result = await AuthService.AuthenticateChildByIdAsync(childId, sequence);
        if (!result.Success)
            _pictureGrid?.ShowError(result.Error ?? "Wrong sequence");
    }
}
```

---

### PictureGridSetup.razor
Picture password setup/configuration component. Used in the setup wizard to let parents create a child's picture password. Displays shuffled grid with selection numbers.

**Parameters:**
```csharp
[Parameter] public int RequiredLength { get; set; } = 4;
[Parameter] public List<string> SelectedImages { get; set; }
[Parameter] public EventCallback<List<string>> SelectedImagesChanged { get; set; }
```

**Public Methods:**
```csharp
void ClearSelection()  // Clear all selections and reshuffle grid
```

**Usage:**
```razor
<PictureGridSetup @bind-SelectedImages="_selectedImages" RequiredLength="4" />
```

---

### AdminPanel.razor
Admin-only user management panel. Extracted from Settings.razor. Displays parent/child lists with admin toggle, and forms to add new parents and children.

**Parameters:**
```csharp
[Parameter, EditorRequired] public int CurrentUserId { get; set; }
[Parameter, EditorRequired] public List<ParentSummary> Parents { get; set; }
[Parameter, EditorRequired] public List<ChildSummary> Children { get; set; }
[Parameter] public EventCallback OnDataChanged { get; set; }
```

**Usage:**
```razor
<AdminPanel CurrentUserId="_currentUserId"
            Parents="_parents"
            Children="_children"
            OnDataChanged="RefreshData" />
```

---

### TransactionList.razor
Displays a list of transactions with type-based icons, descriptions, dates, and color-coded amounts (green for deposits/allowance, red for withdrawals).

**Parameters:**
```csharp
[Parameter] public List<Transaction> Transactions { get; set; }
```

**Usage:**
```razor
<TransactionList Transactions="@_transactions" />
```

---

## Layout Components (`Components/Layout/`)

### MainLayout.razor
Primary application layout. Provides MudBlazor dark theme (primary orange #FF6B35, secondary purple #9B59B6), app bar with greeting and logout button, and main content area.

### EmptyLayout.razor
Minimal layout with MudBlazor theme but no navigation or app bar. Used for the setup wizard and auth pages that need full-width presentation.

### NavMenu.razor
Legacy navigation menu (template component, not actively used).

---

## Utility Components

### RedirectToLogin.razor
Authentication redirect component. Used by the auth system to redirect unauthenticated users to the login page.

---

## Page Patterns

### Parent Child-Context Pages
All pages in `Pages/Parent/Child/` follow this pattern:

1. **Route with childId:** `@page "/parent/child/{ChildId:int}/..."`
2. **ChildContextHeader at top:** Shows child name and back navigation
3. **Load child in OnInitializedAsync:** Validate child exists
4. **Use per-child service methods:** `GetTransactionsForChildAsync(childId)`

**Template:**
```razor
@page "/parent/child/{ChildId:int}/example"
@attribute [Authorize(Roles = "Parent")]

<ChildContextHeader ChildName="@_child?.Name" BackUrl="@AppRoutes.Parent.ChildDetail(ChildId)" />

@if (_isLoading)
{
    <MudProgressCircular Indeterminate="true" />
}
else if (_child == null)
{
    <MudAlert Severity="Severity.Error">Child not found</MudAlert>
}
else
{
    <!-- Page content here -->
}

@code {
    [Parameter] public int ChildId { get; set; }
    [Inject] private IUserService UserService { get; set; } = default!;

    private User? _child;
    private bool _isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        _child = await UserService.GetChildByIdAsync(ChildId);
        _isLoading = false;
    }
}
```

---

## UI Guidelines

### Buttons
Use neumorphic CSS classes for main actions:
```html
<button class="neu-btn neu-btn-primary neu-btn-large">Primary Action</button>
<button class="neu-btn neu-btn-secondary">Secondary Action</button>
```

For MudBlazor buttons (forms, navigation):
```razor
<MudButton Variant="Variant.Filled" Color="Color.Primary">Submit</MudButton>
<MudButton Variant="Variant.Text" Color="Color.Secondary">Cancel</MudButton>
```

### Cards
```html
<div class="neu-card">
    <!-- Content -->
</div>
```

### Form Handling (Enter key support)
Wrap forms in `<form>` tags with `@onsubmit`:
```razor
<form @onsubmit="HandleSubmit" @onsubmit:preventDefault>
    <!-- Form fields -->
    <MudButton ButtonType="ButtonType.Submit">Submit</MudButton>
</form>
```
