Clean Architecture Reference — Post-migration guide for the Juno Bank project.

## Current Architecture

```
src/
├── JunoBank.Domain/          # Layer 1: Entities + Enums (zero dependencies)
├── JunoBank.Application/     # Layer 2: Interfaces, DTOs, Services, Utils
├── JunoBank.Infrastructure/  # Layer 3: AppDbContext, Migrations, Email, BackgroundServices
└── JunoBank.Web/             # Layer 4: Blazor UI, Auth, Constants, wwwroot
```

### Dependency Rules

```
Domain ← Application ← Infrastructure
                     ← Web (references Application + Infrastructure for DI registration)
```

- **Domain** references nothing
- **Application** references only Domain
- **Infrastructure** references Application + Domain
- **Web** references Application + Infrastructure

## Where Does New Code Go?

| Type | Project | Path |
|------|---------|------|
| Entity / Enum | Domain | `Entities/` or `Enums/` |
| Service interface | Application | `Interfaces/` |
| Service implementation | Application | `Services/` |
| DTO | Application | `DTOs/` |
| Utility (business logic) | Application | `Utils/` |
| DbContext / Migration | Infrastructure | `Data/` |
| Email service | Infrastructure | `Email/` |
| Background service | Infrastructure | `BackgroundServices/` |
| Blazor page / component | Web | `Components/Pages/` or `Components/Shared/` |
| Auth provider | Web | `Auth/` |
| UI utility (routes, formatters) | Web | `Utils/` or `Constants/` |

## Architecture Compliance Checklist

When adding new code, verify:

1. **No upward references** — inner layers never reference outer layers
2. **Services depend on `IAppDbContext`** — not concrete `AppDbContext`
3. **Password hashing uses `IPasswordService`** — no direct BCrypt calls
4. **New entities go in Domain** — with no framework dependencies
5. **New interfaces go in Application** — implementations can be in Application or Infrastructure
6. **DI registration in `Program.cs`** — Web wires everything together
7. **Tests reference Application + Infrastructure + Domain** — not Web
