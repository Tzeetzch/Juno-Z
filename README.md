# Juno Bank

A self-hosted virtual allowance app for teaching kids about money management.

Physical money is becoming rare, but kids still need to learn about saving and spending. Juno Bank creates a virtual piggy bank where parents act as the bank — the money is real (redeemable with parents), but without any financial system integration.

## Features

### For Kids
- **Picture password login** — tap 4 images in sequence instead of typing a password
- **Balance dashboard** — see current balance and transaction history
- **Request deposits** — "I got €10 from grandma"
- **Request withdrawals** — "I want €5 for a toy"

### For Parents
- **Approve or deny** money requests with optional notes
- **Manual transactions** — add or subtract money directly
- **Standing orders** — recurring allowance on flexible schedules (hourly, daily, weekly, monthly, yearly)
- **Multi-child support** — manage multiple children from one dashboard
- **Weekly summary email** — configurable per-parent notification with full transaction details
- **User management** — admin panel for managing parents and children
- **Password recovery** — email reset, admin reset, or CLI emergency reset
- **Browser timezone support** — all times displayed in the user's local timezone

### Setup & Security
- **First-run setup wizard** — guided 4-step setup (no default passwords)
- **SMTP configuration** — configure email during setup or from admin settings
- **Login rate limiting** — 5 failed attempts triggers a 5-minute lockout
- **Security hardened** — HttpOnly cookies, CSRF protection, timing-safe comparisons, security headers

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Framework | [Blazor Server](https://learn.microsoft.com/en-us/aspnet/core/blazor/) (.NET 8) |
| UI | [MudBlazor](https://mudblazor.com/) + custom neumorphic styling |
| Database | SQLite + Entity Framework Core |
| Email | MailKit (SMTP) |
| Deployment | Docker / Podman |

## Quick Start

### Development

```bash
cd src/JunoBank.Web
dotnet run
```

On first run, the Setup Wizard will guide you through creating accounts.

### Demo Mode

Set the `JUNO_SEED_DEMO` environment variable to seed test accounts:

```bash
JUNO_SEED_DEMO=true dotnet run
```

| Account | Credentials |
|---------|------------|
| Parent (admin) | dad@junobank.local / parent123 |
| Parent 2 | mom@junobank.local / parent123 |
| Child (Junior) | Tap: cat → dog → star → moon |
| Child (Sophie) | Tap: star → moon → cat → dog |

### Docker

```bash
cd docker
docker-compose up -d --build
```

The app runs on `http://localhost:5050`. See [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md) for production setup with Nginx reverse proxy and SSL.

## Testing

```bash
# Unit tests (xUnit)
dotnet test tests/JunoBank.Tests

# E2E tests (Playwright)
cd tests/e2e
npm test
```

154 unit tests and 73 E2E tests covering authentication, transactions, requests, standing orders, notifications, and the setup wizard.

## Project Structure

```
src/JunoBank.Web/
├── Components/
│   ├── Pages/          # Route-mapped pages (Auth, Child, Parent, Setup)
│   └── Shared/         # Reusable dialog and form components
├── Services/           # Business logic (interfaces + implementations)
├── Data/
│   ├── Entities/       # EF Core entities
│   └── Migrations/     # Database migrations
├── BackgroundServices/ # Allowance scheduler, notification processor
└── Auth/               # Custom auth state provider

tests/
├── JunoBank.Tests/     # xUnit service-level tests
└── e2e/                # Playwright browser tests
```

## Documentation

Developer documentation lives in the `docs/` directory:

- [Architecture](docs/ARCHITECTURE.md) — tech decisions, entities, structure
- [Components](docs/COMPONENTS.md) — reusable UI components
- [API Reference](docs/API.md) — services and entities
- [Routes](docs/ROUTES.md) — URL to page mapping
- [Conventions](docs/CONVENTIONS.md) — coding standards
- [Testing](docs/TESTING.md) — test patterns
- [Deployment](docs/DEPLOYMENT.md) — Docker production setup
- [Decisions](docs/DECISIONS.md) — architectural decision log

Project history and changelog are in the [GitHub Wiki](https://github.com/Tzeetzch/Juno-Z/wiki).

## License

Private project — not open source.
