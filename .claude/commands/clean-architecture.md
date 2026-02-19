Clean Architecture Specialist - Restructure the project toward Clean Architecture.

**Mode:** Incremental (never break the build; migrate layer by layer)

**Goal:** Migrate from the current single-project Blazor Server structure to Clean Architecture with proper dependency inversion and separation of concerns.

## Target Structure

```
JunoBank/
├── src/
│   ├── JunoBank.Domain/              # Layer 1: Entities & business rules (zero dependencies)
│   │   ├── Entities/                 # User, Transaction, MoneyRequest, ScheduledAllowance, etc.
│   │   ├── Enums/                    # UserRole, TransactionType, RequestStatus, etc.
│   │   └── Exceptions/              # Domain-specific exceptions
│   │
│   ├── JunoBank.Application/         # Layer 2: Use cases & interfaces
│   │   ├── Interfaces/              # IUserService, IAuthService, IAllowanceService, etc.
│   │   ├── DTOs/                    # ChildDashboardData, ParentDashboardData, ChildSummary, etc.
│   │   └── Services/                # Service implementations (business logic)
│   │
│   ├── JunoBank.Infrastructure/      # Layer 3: External concerns
│   │   ├── Data/                    # AppDbContext, Migrations, entity configs
│   │   ├── Email/                   # EmailService, EmailConfigService
│   │   └── BackgroundServices/      # AllowanceBackgroundService
│   │
│   └── JunoBank.Web/                 # Layer 4: Presentation (Blazor)
│       ├── Components/              # Pages, Layout, Shared
│       ├── Auth/                    # CustomAuthStateProvider, UserSession
│       └── wwwroot/                 # Static assets
│
├── tests/
│   ├── JunoBank.Tests/              # Unit tests (Domain + Application)
│   └── e2e/                         # Playwright E2E tests
└── docs/
```

## Dependency Rules

```
Domain ← Application ← Infrastructure
                     ← Web (references Application + Infrastructure for DI registration)
```

- **Domain** references NOTHING (no NuGet packages except maybe annotations)
- **Application** references only Domain
- **Infrastructure** references Application + Domain (implements interfaces)
- **Web** references Application + Infrastructure (for DI wiring only)
- **NEVER** let inner layers reference outer layers

## Migration Process

Follow this order strictly. Each step must compile and pass tests before moving to the next.

### Step 1: Create Domain Layer
1. Create `JunoBank.Domain` project
2. Move entities from `JunoBank.Core/Data/Entities/` → `JunoBank.Domain/Entities/`
3. Move enums to `JunoBank.Domain/Enums/`
4. Remove ALL framework dependencies from entities (no EF annotations on domain objects)
5. Update namespaces
6. Run `/build` and `/test`

### Step 2: Create Application Layer
1. Create `JunoBank.Application` project (references Domain only)
2. Move service interfaces (`IUserService`, `IAuthService`, etc.) → `JunoBank.Application/Interfaces/`
3. Move DTOs (`ChildDashboardData`, `ParentDashboardData`, etc.) → `JunoBank.Application/DTOs/`
4. Move service implementations → `JunoBank.Application/Services/`
5. Services should depend on abstractions (e.g., `IAppDbContext` or repository interfaces), not concrete DbContext
6. Run `/build` and `/test`

### Step 3: Create Infrastructure Layer
1. Create `JunoBank.Infrastructure` project (references Application + Domain)
2. Move `AppDbContext`, migrations, entity configurations → `JunoBank.Infrastructure/Data/`
3. Move `EmailService`, `EmailConfigService` → `JunoBank.Infrastructure/Email/`
4. Move `AllowanceBackgroundService` → `JunoBank.Infrastructure/BackgroundServices/`
5. EF Core fluent API configs go here (not on domain entities)
6. Create `IAppDbContext` interface in Application, implement in Infrastructure
7. Run `/build` and `/test`

### Step 4: Slim Down Web Layer
1. Web project should only contain: Components, Auth, Program.cs, wwwroot
2. Move any remaining business logic out of components into Application services
3. Register all DI in `Program.cs` using extension methods from each layer
4. Run `/build` and `/test`

### Step 5: Verify & Document
1. Run full test suite: `/test`
2. Run architecture check: `/architect`
3. Update `docs/ARCHITECTURE.md` with new structure
4. Update `CLAUDE.md` if paths changed

## Rules

- **One step at a time** — never move to the next step until current step compiles and tests pass
- **No behavior changes** — all existing tests must continue to pass throughout
- **Preserve git history** — use `git mv` where possible for file moves
- **Update namespaces** — find and replace across the entire solution after each move
- **Keep EF Core OUT of Domain** — use fluent API in Infrastructure, not data annotations
- **DTOs cross boundaries** — domain entities should NOT be returned directly from API/pages
- **Ask before large moves** — confirm with user before restructuring each layer

## What NOT to Do

- Don't add MediatR/CQRS unless explicitly asked — it's overkill for this app size
- Don't introduce repository pattern over EF Core unless explicitly asked — DbContext is already a unit of work
- Don't change business logic while restructuring — that's a separate task
- Don't rename files unnecessarily — focus on folder structure and namespaces
- Don't gold-plate — the goal is proper separation, not enterprise astronautics

## Checking Your Work

After each step, verify:
1. `dotnet build` — zero errors, zero warnings
2. `dotnet test` — all tests pass
3. No circular references between projects
4. Inner layers have no `using` statements from outer layers
