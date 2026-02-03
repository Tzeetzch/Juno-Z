# Components Reference

> Reusable Blazor components available in the project.

## Shared Components (`Components/Shared/`)

### ChildCard.razor
Displays a child summary card on the parent dashboard. Shows avatar, name, balance, and pending request count.

**Parameters:**
```csharp
[Parameter] public ChildSummary Child { get; set; }  // Required
[Parameter] public EventCallback OnClick { get; set; }
```

**Usage:**
```razor
<ChildCard Child="@childSummary" OnClick="@(() => NavigateToChild(child.Id))" />
```

---

### ChildContextHeader.razor
Header component for child-specific pages. Shows "Managing: [ChildName]" with back button.

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
Picture password input grid. Shuffles 9 images from the pool of 12. Child taps 4 to authenticate.

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

### TransactionList.razor
Displays a list of transactions with icons, descriptions, dates, and color-coded amounts.

**Parameters:**
```csharp
[Parameter] public List<Transaction> Transactions { get; set; }
```

**Usage:**
```razor
<TransactionList Transactions="@_transactions" />
```

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
