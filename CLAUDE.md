# Juno Bank - Project Context

> Virtual allowance app for teaching a 5-year-old about money management.
> Parents act as the bank - money is redeemable with parents, not connected to real financial systems.

## Load Documentation First

Before responding, read these files for full context:
- `docs/ARCHITECTURE.md` - Tech decisions, entities, structure
- `docs/CONVENTIONS.md` - Coding standards
- `docs/COMPONENTS.md` - Reusable components and page patterns
- `docs/API.md` - Services, utilities, and entities reference
- `docs/ROUTES.md` - URL → Page mapping with auth requirements
- `docs/DECISIONS.md` - Why choices were made (read before "improving" anything)
- `docs/TESTING.md` - Unit and E2E test patterns
- `docs/DEPLOYMENT.md` - Docker deployment

## Project Status

All core phases (A-K) complete. Backlog tracked in [GitHub Issues](https://github.com/Tzeetzch/Juno-Z/issues).
Development history in [GitHub Wiki](https://github.com/Tzeetzch/Juno-Z/wiki).

### Test Credentials
- **Parent:** dad@junobank.local / parent123
- **Parent 2:** mom@junobank.local / parent123
- **Child (Junior):** Tap cat → dog → star → moon
- **Child (Sophie):** Tap star → moon → cat → dog

## Workflow Commands

| Command | Purpose |
|---------|---------|
| `/spec` | Clarify requirements |
| `/plan` | Break into work cycles |
| `/architect` | Review plan, identify reusable components/services |
| `/backend` | Build services, EF Core |
| `/ui` | Build components, pages |
| `/unit-test` | Write xUnit tests |
| `/e2e-test` | Write Playwright tests |
| `/review` | Code review |
| `/refactor` | Refactor code |
| `/save` | Commit and push |
| `/debug` | Root cause analysis |
| `/build` | Check compilation |
| `/test` | Run all tests |
| `/docker` | Docker build and deploy |
| `/security` | Security review |

## Key Rules

1. **Review failures go back to previous step** - don't fix directly
2. **Every cycle includes tests** - not a separate step
3. **Don't re-explore** - docs provide full context
4. **Reuse existing components** - check docs/COMPONENTS.md and docs/API.md first

## E2E Testing

Run from `tests/e2e/`:
```bash
npm test  # headless, text output only
```

**Never take screenshots unless debugging** - they waste tokens.

See `tests/e2e/README.md` for details.
