# Juno Bank - Project Context

> Virtual allowance app for teaching a 5-year-old about money management.
> Parents act as the "bank" - money is redeemable with parents, not connected to real financial systems.

## ⚠️ IMPORTANT: Load All Documentation First

**Before responding to any request, ALWAYS read these files:**
- `docs/ARCHITECTURE.md` - Full architecture and design decisions
- `docs/SCAFFOLDING.md` - Implementation phases and current status
- `docs/CONVENTIONS.md` - Coding standards and naming conventions
- `docs/DEPLOYMENT.md` - Docker and deployment instructions

This ensures you have complete context for the project. The information below is a summary only.

## Workflow: Product Owner Mode

The user acts as **Product Owner** - they define what to build and review results.
Claude acts as the **Development Team** - handles implementation, testing, and quality.

**Lean workflow (default):**
1. `/spec` - Understand the request
2. `/plan` - Break into cycles
3. **Per cycle:** Build → Show → You approve → Next cycle
4. `/save` - When you say so

**Call specialists when needed:**
- `/backend`, `/ui` - When building that layer
- `/unit-test` - After building logic (not for tiny UI tweaks)
- `/review` - Before /save
- `/architect` - Periodic check, or after major features
- `/security` - Only for auth/data changes
- `/debug` - Only when something breaks
- `/e2e-test` - End of feature, not every cycle
- `/refactor` - When code gets messy

**Quality standards:**
- Always write tests for new features
- Run build after significant changes
- Follow the coding conventions in docs/CONVENTIONS.md
- Keep code simple - avoid over-engineering

**Available commands:**

| Command | When | Purpose |
|---------|------|---------|
| **Always** |||
| `/spec` | Every feature | Clarify requests before building |
| `/plan` | Every feature | Break into small work cycles |
| `/review` | Before /save | Quick sanity check |
| `/save` | When you say | Commit and push |
| **While building** |||
| `/backend` | Backend work | Services, EF Core, logic |
| `/ui` | Frontend work | Components, styling |
| `/unit-test` | New logic | Write tests for services |
| **Situational** |||
| `/architect` | Major features | Check structure, teaches good design |
| `/security` | Auth/data changes | Check for vulnerabilities |
| `/e2e-test` | Feature complete | Full user flow tests |
| `/debug` | Something breaks | Find root cause |
| `/refactor` | Code is messy | Clean up without changing behavior |
| **Ops** |||
| `/build` | Check compilation | Build and report errors |
| `/test` | Run all tests | Verify nothing broke |
| `/docker` | Deployment | Container operations |

## Project Status
- **Completed:** Phase A (project setup), Phase B (database entities), Phase C (authentication)
- **Next:** Phase D (Child Features) - Say "Start Phase D" to continue
- **Full docs:** See ARCHITECTURE.md, SCAFFOLDING.md, CONVENTIONS.md, DEPLOYMENT.md (already loaded)

### What's Built
- Blazor Server project at `src/JunoBank.Web/`
- MudBlazor + dark theme (orange/purple)
- Neumorphic CSS (`wwwroot/css/neumorphic.css`)
- SQLite database with entities: User, Transaction, MoneyRequest, ScheduledAllowance, PicturePassword
- Seed data: Dad, Mom (parents), Junior (child with €10)
- Authentication: Parent login (email/password), Child login (picture password)
- Route protection with [Authorize]
- **E2E Testing:** Playwright setup in `tests/e2e/` (text-based, token-efficient)

### Test Credentials
- **Parent:** dad@junobank.local / parent123
- **Child:** Tap cat→dog→star→moon

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
| **E2E Testing** | **Playwright (text-based)** | **Token-efficient: text output only, no screenshots unless debugging** |

## Design Requirements
- **Theme:** Dark mode
- **Primary color:** Orange (#FF6B35)
- **Accent color:** Royal purple (#9B59B6)
- **Buttons:** Semi-3D neumorphic style (soft shadows)
- **Child UI:** Kid-friendly, large touch targets, not overwhelming

## Solution Structure
```
Juno-Z/
├── src/JunoBank.Web/              # Blazor Server project
│   ├── Components/
│   │   ├── Layout/                # MainLayout (MudBlazor themed)
│   │   └── Pages/                 # Home (neumorphic demo)
│   ├── Data/
│   │   ├── Entities/              # User, Transaction, MoneyRequest, etc.
│   │   ├── AppDbContext.cs
│   │   └── DbInitializer.cs       # Seed data
│   ├── wwwroot/css/neumorphic.css # 3D button styles
│   └── Program.cs
├── tests/e2e/                     # Playwright E2E tests (Node.js project)
│   ├── specs/                     # Test files (.spec.ts)
│   ├── playwright.config.ts       # Test configuration
│   └── package.json               # Node dependencies
├── docs/                          # Architecture, conventions, deployment
└── .claude/commands/              # Team commands (/spec, /plan, etc.)
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

## E2E Testing with Playwright

**⚠️ IMPORTANT: Token-Efficient Testing Protocol**

When running E2E tests (`/e2e-test` or manual request):
1. Navigate to `tests/e2e/` directory
2. Run `npm test` (auto-starts server, headless mode, text output)
3. Parse the **console output** for pass/fail results
4. Report results as text (e.g., "5 passed, 2 failed")
5. **NEVER** take screenshots or read images unless explicitly asked
6. **NEVER** use `--headed` mode unless user requests it
7. Only suggest `npm run test:ui` or screenshots if debugging a specific failure

**Why:** Screenshots consume 1000+ tokens each. Text output uses ~100 tokens total.

See `tests/e2e/README.md` for full testing documentation.
