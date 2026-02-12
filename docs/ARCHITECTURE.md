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
- [x] Picture password login (tap images in sequence)
- [x] View current balance (prominent, visual)
- [x] Transaction history (simple, visual)
- [x] Request withdrawal ("I want €X for Y")
- [x] Request deposit ("I got €X from grandma")

**Parent Features:**
- [x] Standard authentication (email/password)
- [x] Manual transactions (add/subtract with notes)
- [x] Scheduled allowance (automatic, flexible intervals)
- [x] Approve/deny pending requests
- [x] Email notifications for new requests
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
│       │   ├── Layout/              # MainLayout, EmptyLayout, NavMenu
│       │   ├── Pages/
│       │   │   ├── Auth/            # Login, ParentLogin, ForgotPassword, ResetPassword
│       │   │   ├── Child/           # Dashboard, RequestDeposit, RequestWithdrawal
│       │   │   ├── Parent/          # Dashboard, PendingRequests, ManualTransaction, Settings
│       │   │   ├── Parent/Child/    # ChildDetail, ChildManualTransaction, ChildOrderEditor, etc.
│       │   │   └── Setup/           # SetupWizard, SetupStep1-4, SetupComplete
│       │   └── Shared/              # ChildCard, ChildSelector, ChildContextHeader,
│       │                            # PictureGrid, PictureGridSetup, TransactionList
│       ├── Data/
│       │   ├── AppDbContext.cs
│       │   ├── DbInitializer.cs     # Demo data seeding (JUNO_SEED_DEMO=true)
│       │   ├── Entities/            # User, Transaction, MoneyRequest, ScheduledAllowance, etc.
│       │   └── Migrations/
│       ├── Services/                # IAuthService, IUserService, IAllowanceService, ISetupService, etc.
│       ├── BackgroundServices/      # AllowanceBackgroundService (scheduled tasks)
│       ├── Auth/                    # CustomAuthStateProvider, UserSession
│       ├── Constants/               # PicturePasswordImages
│       ├── Utils/                   # AppRoutes, CurrencyFormatter, SecurityUtils, StatusDisplayHelper
│       └── wwwroot/                 # app.css, css/neumorphic.css
├── docker/
│   ├── Dockerfile
│   ├── docker-compose.yml
│   └── nginx-example.conf
├── docs/                            # This folder
└── tests/
    ├── e2e/                         # Playwright E2E tests (64 specs, 13 files)
    └── JunoBank.Tests/              # xUnit unit tests (96 tests, 8 files)
```

### Database Entities

```
User
├── Id, Name, Role (Parent/Child)
├── IsAdmin (system admin flag)
├── Email, PasswordHash (parents only)
├── FailedLoginAttempts, LockoutUntil (parent rate limiting)
├── PicturePassword (child only, navigation)
├── Birthday (optional, for children)
├── Balance
└── CreatedAt

PicturePassword
├── Id, UserId
├── ImageSequenceHash (SHA256, Base64)
├── GridSize (default 9), SequenceLength (default 4)
├── FailedAttempts, LockedUntil
└── User (navigation)

Transaction
├── Id, UserId, Amount
├── Type (Deposit/Withdrawal/Allowance)
├── Description, CreatedAt, IsApproved
└── ApprovedByUserId

MoneyRequest
├── Id, ChildId, Amount, Type, Description
├── Status (Pending/Approved/Denied)
├── ResolvedByUserId, ParentNote, ResolvedAt
└── CreatedAt

ScheduledAllowance
├── Id, ChildId, CreatedByUserId
├── Amount, Interval (Hourly/Daily/Weekly/Monthly/Yearly)
├── DayOfWeek, DayOfMonth, MonthOfYear, TimeOfDay
├── Description (custom text for transactions)
├── NextRunDate, LastRunDate, IsActive
└── CreatedAt

PasswordResetToken
├── Id, UserId
├── Token (unique), ExpiresAt (15 min)
├── UsedAt, CreatedAt
└── User (navigation)
```

### Services

| Service | Purpose |
|---------|---------|
| `IAuthService` | Parent/child authentication, session management, rate limiting |
| `IUserService` | User CRUD, balances, transactions, requests, multi-child support |
| `IAllowanceService` | Standing order CRUD, processing, schedule calculation |
| `ISetupService` | First-run setup wizard (check/complete) |
| `IPasswordService` | BCrypt password hashing abstraction |
| `IPasswordResetService` | Token generation, validation, password reset |
| `IEmailService` | Email sending (SMTP or console fallback) |

### Background Services

| Service | Schedule | Purpose |
|---------|----------|---------|
| `AllowanceBackgroundService` | Every 60 sec | Check for due allowances, process catch-up |

### Key NuGet Packages

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.*" />
<PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
<PackageReference Include="MailKit" Version="4.14.1" />
<PackageReference Include="MudBlazor" Version="8.15.0" />
```

### Picture Password Flow

1. Display 3×3 grid of images (shuffled from pool of 12)
2. Child taps 4 images in their secret sequence
3. Sequence is SHA256 hashed and compared to stored hash
4. Lock out after 5 failed attempts for 5 minutes (auto-unlock)

### Docker Deployment

```yaml
services:
  junobank:
    build: .
    ports:
      - "5050:5050"
    volumes:
      - junobank-data:/app/data   # SQLite persistence
      - junobank-keys:/app/keys   # Data Protection keys
    restart: unless-stopped
```
