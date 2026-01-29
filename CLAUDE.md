# Juno Bank - Project Context

> Virtual allowance app for teaching a 5-year-old about money management.
> Parents act as the "bank" - money is redeemable with parents, not connected to real financial systems.

## Project Status
- **Phase:** Architecture planning complete, ready for Phases 3-8 (workflow, CI/CD, conventions, scaffolding)
- **Full architecture:** `docs/ARCHITECTURE.md`

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

## Remaining Planning Phases
- Phase 3: Claude Code skills/agents for dev workflow
- Phase 4: GitHub workflow and CI/CD
- Phase 5: Deployment strategy details
- Phase 6: Coding conventions
- Phase 7: Documentation approach
- Phase 8: Scaffolding plan

## Development Notes
- This is a learning project - no rush, focus on understanding Blazor
- Keep it simple - avoid over-engineering for a 3-user family app
- Responsive design required (works on tablet in store)
