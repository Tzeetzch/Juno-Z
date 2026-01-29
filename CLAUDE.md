# Juno Bank - Project Context

> Virtual allowance app for teaching a 5-year-old about money management.
> Parents act as the "bank" - money is redeemable with parents, not connected to real financial systems.

## Workflow: Product Owner Mode

The user acts as **Product Owner** - they define what to build and review results.
Claude acts as the **Development Team** - handles implementation, testing, and quality.

**When given a feature request:**
1. Clarify requirements if ambiguous
2. Plan the implementation (update docs/ARCHITECTURE.md if needed)
3. Write the code with tests
4. Run `/build` and `/test` to verify
5. Present a summary of what was built for review

**Quality standards:**
- Always write tests for new features
- Run build after significant changes
- Follow the coding conventions in docs/CONVENTIONS.md
- Keep code simple - avoid over-engineering

**Available commands:**

| Command | Role | Purpose |
|---------|------|---------|
| `/architect` | Architecture Guardian | Review structure, patterns, teach good design |
| `/ui` | UI Specialist | Blazor components, styling, responsiveness |
| `/backend` | Backend Specialist | Services, EF Core, business logic |
| `/unit-test` | Unit Tester | Write xUnit tests for services |
| `/e2e-test` | E2E Tester | Integration tests, user flow tests |
| `/refactor` | Refactorer | Clean up code without changing behavior |
| `/review` | Code Reviewer | Critical review before commits |
| `/build` | CI | Build and check for errors |
| `/test` | CI | Run all tests |
| `/docker` | DevOps | Container operations |
| `/save` | DevOps | Commit and push to GitHub |

## Project Status
- **Phase:** All planning complete. Ready to scaffold.
- **Next:** Say "Start Phase A scaffolding" to begin implementation
- **Full docs:** `docs/ARCHITECTURE.md`, `docs/SCAFFOLDING.md`, `docs/CONVENTIONS.md`, `docs/DEPLOYMENT.md`

## Users
| User | Auth | Capabilities |
|------|------|--------------|
| Child (5yo) | Picture password (tap 4 images) | View balance, history, request withdraw/deposit |
| Parent 1 | Email/password | Approve requests, manual transactions, configure allowance |
| Parent 2 | Email/password | Same as Parent 1 |

## Core Features (MVP)
- **Child:** Balance view, transaction history, request withdrawal ("I want €5 for X"), request deposit ("Got €10 from grandma")
- **Parents:** Approve/deny requests, manual add/subtract, scheduled weekly allowance, email + in-app notifications

## Phase 2 (Post-MVP)
- Savings goals with visual progress
- "How long to save for X" calculator

## Architecture Decisions

| Aspect | Decision | Why |
|--------|----------|-----|
| Framework | Blazor Server (.NET 8) | Learning project; real-time SignalR updates; 3 users on home network |
| Database | SQLite + EF Core | Zero config; single-file backup; Docker-friendly |
| Parent Auth | Custom cookie auth + BCrypt | ASP.NET Identity overkill for 3 users |
| Child Auth | Picture password (SHA256 hash) | Kid-friendly; 3x3 image grid, tap 4 in sequence |
| UI | MudBlazor + custom neumorphic CSS | Best docs for beginners; easy theming |
| Scheduled Tasks | BackgroundService | Built-in .NET; handles weekly allowance |
| Email | MailKit + SMTP | Works with Gmail, SendGrid, etc. |
| State | Scoped services + events | Simple; no Fluxor/Redux needed |
| Hosting | Docker (self-hosted) | User has Docker-capable infrastructure |

## Design Requirements
- **Theme:** Dark mode
- **Primary color:** Orange (#FF6B35)
- **Accent color:** Royal purple (#9B59B6)
- **Buttons:** Semi-3D neumorphic style (soft shadows)
- **Child UI:** Kid-friendly, large touch targets, not overwhelming

## Solution Structure
```
JunoPiggyBank/
├── src/JunoPiggyBank.Web/     # Blazor Server project
│   ├── Components/Pages/Child/   # Dashboard, History, RequestMoney
│   ├── Components/Pages/Parent/  # Dashboard, Requests, Settings
│   ├── Data/Entities/            # User, Transaction, MoneyRequest
│   ├── Services/                 # Business logic
│   └── Auth/                     # CustomAuthStateProvider
├── docker/                    # Dockerfile, docker-compose.yml
└── tests/
```

## Key Packages
```xml
Microsoft.EntityFrameworkCore.Sqlite (8.0.*)
BCrypt.Net-Next (4.0.*)
MailKit (4.3.*)
MudBlazor (6.11.*)
```

## Development Notes
- This is a learning project - no rush, focus on understanding Blazor
- Keep it simple - avoid over-engineering for a 3-user family app
- Responsive design required (works on tablet in store)
