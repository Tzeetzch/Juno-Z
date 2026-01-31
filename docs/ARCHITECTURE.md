# Juno Bank - Architecture & Planning

## Phase 1: Requirements Summary

### Problem Statement
Physical money is becoming rare, but parents want to teach their 5-year-old about money management. This app creates a "virtual allowance" system where parents act as the bank - the money is real in terms of what the child can redeem, but without actual financial system integration.

### Users
| User | Auth Method | Role |
|------|-------------|------|
| Child (5yo) | Picture password | View balance, request transactions |
| Parent 1 | Standard login | Bank admin - approve, manage, configure |
| Parent 2 | Standard login | Bank admin - approve, manage, configure |

### Core Features (MVP)

**Child Features:**
- [ ] Picture password login (tap images in sequence)
- [ ] View current balance (prominent, visual)
- [ ] Transaction history (simple, visual)
- [ ] Request withdrawal ("I want €X for Y")
- [ ] Request deposit ("I got €X from grandma")

**Parent Features:**
- [ ] Standard authentication (email/password)
- [ ] Manual transactions (add/subtract with notes)
- [ ] Scheduled weekly allowance (automatic)
- [ ] Approve/deny pending requests
- [ ] Email notifications for new requests
- [ ] In-app notification center

### Phase 2 Features (Post-MVP)
- [ ] Savings goals with visual progress bars
- [ ] "How long to save for X" calculator
- [ ] Visual comparison of wants vs. balance

### Technical Constraints
- **Hosting:** Self-hosted via Docker
- **Devices:** Responsive - works on computer, tablet, phone (including "in store" mobile use)
- **Timeline:** No rush - this is a learning project
- **Framework:** Blazor (.NET) - learning focus

### Design Requirements
- Dark mode (reduce eye strain)
- **Primary color:** Orange (#FF6B35)
- **Accent color:** Royal purple (#9B59B6)
- Semi-3D rounded buttons (soft neumorphism style)
- Kid-friendly but not overwhelming

---

## Phase 2: Architecture Design

### Technology Decisions

| Aspect | Choice | Rationale |
|--------|--------|-----------|
| **Hosting Model** | Blazor Server | Simplest for learning; real-time updates via SignalR; all 3 users on home network = no latency issues; code stays server-side (security) |
| **Database** | SQLite | Zero config; single file backup; Docker-friendly; EF Core support |
| **Auth (Parents)** | Custom cookie auth + BCrypt | ASP.NET Identity is overkill for 3 users |
| **Auth (Child)** | Picture password (SHA256 hash) | Age-appropriate; tap 4 images in sequence |
| **UI Library** | MudBlazor + custom CSS | Best docs for beginners; easy theming; add neumorphic styles on top |
| **Scheduled Tasks** | BackgroundService | Built-in .NET; no external dependencies for one weekly task |
| **Email** | MailKit + SMTP | Microsoft-recommended; works with Gmail, SendGrid, etc. |
| **State Management** | Scoped services + events | Fluxor/Redux overkill for small app |

### Solution Structure

```
JunoBank/
├── src/
│   └── JunoBank.Web/                # Single Blazor Server project
│       ├── Components/
│       │   ├── Layout/              # MainLayout, NavMenu
│       │   ├── Pages/
│       │   │   ├── Child/           # Dashboard, History, RequestMoney
│       │   │   ├── Parent/          # Dashboard, Requests, Transactions, Settings
│       │   │   └── Auth/            # Login, PictureLogin
│       │   └── Shared/              # BalanceCard, TransactionList, PictureGrid
│       ├── Data/
│       │   ├── AppDbContext.cs
│       │   ├── Entities/            # User, Transaction, MoneyRequest, etc.
│       │   └── Migrations/
│       ├── Services/                # IAllowanceService, IUserService, IDateTimeProvider
│       ├── BackgroundServices/      # AllowanceBackgroundService (scheduled tasks)
│       ├── Auth/                    # CustomAuthStateProvider
│       └── wwwroot/css/             # app.css, neumorphic.css
├── docker/
│   ├── Dockerfile
│   └── docker-compose.yml
├── docs/                            # This folder
└── tests/
    ├── e2e/                         # Playwright E2E tests (53 specs)
    └── JunoBank.Tests/              # xUnit unit tests (18 tests)
```

### Database Entities

```
User
├── Id, Name, Role (Parent/Child)
├── Email, PasswordHash (parents only)
├── PicturePassword (child only)
└── Balance

Transaction
├── UserId, Amount, Type (Deposit/Withdrawal/Allowance)
├── Description, CreatedAt, IsApproved
└── ApprovedByParentId

MoneyRequest
├── ChildId, Amount, Type, Description
├── Status (Pending/Approved/Denied)
└── ResolvedByParentId, ParentNote

ScheduledAllowance
├── ChildId, CreatedByParentId
├── Amount, DayOfWeek, TimeOfDay
├── Description (custom text for transactions)
├── NextRunDate, LastRunDate, IsActive
└── CreatedAt

### Services

| Service | Purpose |
|---------|---------|
| `IUserService` | User lookup, authentication, balance updates |
| `IAllowanceService` | Allowance CRUD, processing, catch-up logic |
| `IDateTimeProvider` | Mockable DateTime.Now for unit testing |

### Background Services

| Service | Schedule | Purpose |
|---------|----------|---------|
| `AllowanceBackgroundService` | Every 60 sec | Check for due allowances, process catch-up |
```

### Key NuGet Packages

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.*" />
<PackageReference Include="BCrypt.Net-Next" Version="4.0.*" />
<PackageReference Include="MailKit" Version="4.3.*" />
<PackageReference Include="MudBlazor" Version="6.11.*" />
```

### Picture Password Flow

1. Display 3×3 grid of colorful images (animals, objects)
2. Child taps 4 images in their secret sequence
3. Sequence is SHA256 hashed and compared to stored hash
4. Lock out after 5 failed attempts (parent resets)

### Docker Deployment

```yaml
services:
  junobank:
    build: .
    ports:
      - "8080:8080"
    volumes:
      - junobank-data:/app/data  # SQLite persistence
    restart: unless-stopped
```

---

## Phases 3-8: Complete

| Phase | Topic | Document |
|-------|-------|----------|
| 3 | Claude Code workflow | `.claude/commands/` + CLAUDE.md |
| 4 | CI/CD | `.github/workflows/release.yml` |
| 5 | Deployment | `docs/DEPLOYMENT.md` |
| 6 | Coding conventions | `docs/CONVENTIONS.md` |
| 7 | Documentation | All docs/ files |
| 8 | Scaffolding | `docs/SCAFFOLDING.md` |

## Next Step

Ready to scaffold. Say: **"Start Phase A scaffolding"**
