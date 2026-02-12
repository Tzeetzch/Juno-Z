# Juno Bank

A virtual allowance app for teaching kids about money management.

## What is this?

Physical money is becoming rare, but kids still need to learn about saving and spending. Juno Bank creates a "virtual piggy bank" where parents act as the bank - the money is real (redeemable with parents), but without actual financial system integration.

## Features

**For Kids:**
- Picture password login (tap images in sequence)
- View balance and transaction history
- Request withdrawals ("I want €5 for a toy")
- Request deposits ("I got €10 from grandma")

**For Parents:**
- Approve/deny money requests
- Manual transactions
- Flexible standing orders (hourly/daily/weekly/monthly/yearly)
- Email notifications
- Multi-child support
- User management (admin)

## Tech Stack

- **Frontend:** Blazor Server (.NET 8)
- **Database:** SQLite + Entity Framework Core
- **UI:** MudBlazor 8.x + custom neumorphic styling
- **Deployment:** Docker (self-hosted)

## Status

**Current:** Phase K (First-Run Setup Wizard) - Phases A-J complete.

## Quick Start

```bash
cd src/JunoBank.Web
dotnet run
# Open https://localhost:5001
```

**Demo Mode** (set `JUNO_SEED_DEMO=true` env var):
- Parent: `dad@junobank.local` / `parent123`
- Child: Tap cat → dog → star → moon

Without demo mode, the Setup Wizard will guide you through creating accounts.

See [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md) for Docker production setup.

## Design

- Dark mode
- Orange primary / Purple accent
- Neumorphic (soft 3D) buttons
- Kid-friendly, large touch targets
