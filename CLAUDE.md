# Juno Bank - Project Context

> Virtual allowance app for teaching a 5-year-old about money management.
> Parents act as the bank - money is redeemable with parents, not connected to real financial systems.

## Load Documentation First

Before responding, read these files for full context:
- `docs/ARCHITECTURE.md` - Tech decisions, entities, structure
- `docs/STATUS.md` - Current phase and open tickets
- `docs/CONVENTIONS.md` - Coding standards
- `docs/COMPONENTS.md` - Reusable components and page patterns
- `docs/SERVICES.md` - Services, utilities, and entities reference
- `docs/DEPLOYMENT.md` - Docker deployment

## Project Status

**Completed:** Phases A-J (setup through multi-child support)  
**Next:** Phase K (First-Run Setup Wizard)  
**Parked:** Request notifications, production email config

### Test Credentials
- **Parent:** dad@junobank.local / parent123
- **Child (Junior):** Tap cat → dog → star → moon
- **Child (Sophie):** Tap star → moon → cat → dog

## Workflow Commands

| Command | Purpose |
|---------|---------|
| `/spec` | Clarify requirements |
| `/plan` | Break into work cycles |
| `/ba-review` | BA validates the plan |
| `/backend` | Build services, EF Core |
| `/ui` | Build components, pages |
| `/unit-test` | Write xUnit tests |
| `/e2e-test` | Write Playwright tests |
| `/architect` | Structure review |
| `/ux-review` | Design review |
| `/tech-writer` | Update documentation |
| `/save` | Commit and push |
| `/debug` | Root cause analysis |
| `/build` | Check compilation |
| `/test` | Run all tests |

## Key Rules

1. **Review failures go back to previous step** - don't fix directly
2. **Every cycle includes tests** - not a separate step
3. **Don't re-explore** - docs provide full context
4. **Keep it simple** - this is a 3-user family app

## E2E Testing

Run from `tests/e2e/`:
```bash
npm test  # headless, text output only
```

**Never take screenshots unless debugging** - they waste tokens.

See `tests/e2e/README.md` for details.