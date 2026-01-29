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
- Automatic weekly allowance
- Email notifications

## Tech Stack

- **Frontend:** Blazor Server (.NET 8)
- **Database:** SQLite + Entity Framework Core
- **UI:** MudBlazor + custom neumorphic styling
- **Deployment:** Docker (self-hosted)

## Status

🚧 **Planning phase** - Architecture complete, implementation pending.

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for full details.

## Design

- Dark mode
- Orange primary / Purple accent
- Neumorphic (soft 3D) buttons
- Kid-friendly, large touch targets
