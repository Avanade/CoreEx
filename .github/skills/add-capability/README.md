# Add Capability

Retrofits an existing CoreEx domain with messaging and integration support — adding only what's missing rather than regenerating the domain from scratch.

## When to use

| Scenario | Use this | Not this |
|----------|----------|----------|
| Domain exists, needs reliable event publishing | `/add-capability` | — |
| Domain exists, needs to consume events from other services | `/add-capability` | — |
| Creating a new domain from nothing | — | `/scaffold-domain-from-templates` or `/generate-domain` |
| New domain with messaging included from the start | — | `/scaffold-domain-from-templates` (Subscribe + Outbox Relay are options there) |

## How to invoke

**Claude Code:**
```
/add-capability
```

**GitHub Copilot Chat:**
```
#file:.github/skills/add-capability/SKILL.md  add relay and subscribers to Contoso Orders
```

The skill will inspect the existing domain, ask only what it cannot infer, then apply targeted edits.

## Retrofit modes

| Mode | When | What gets added |
|------|------|----------------|
| **A — Outbox Relay** | Domain writes data but has no reliable event publishing | `*.Outbox.Relay` project; relay + Service Bus publisher wiring; outbox migration + `dbex.yaml` alignment |
| **B — Subscribe** | Domain needs to consume events from other services | `*.Subscribe` project; Service Bus receiver; `SubscribedBase<T>` subscriber classes; hosted service wiring |
| **C — Both** | Domain needs to publish and consume | Modes A and B combined |
| **D — Subscribers only** | Subscribe host exists but subscriber classes or registration are incomplete | New subscriber classes and registration only — no host changes |

## What the skill inspects before asking

- Which `*.Api`, `*.Outbox.Relay`, and `*.Subscribe` projects already exist.
- Database engine in use — SQL Server or PostgreSQL — from package references and `Program.cs` wiring.
- Whether outbox infrastructure is already present (migration file + `dbex.yaml outbox: true`).
- Whether reference data is present (`ReferenceDataService` / `*.CodeGen` project) — affects Subscribe wiring.
- Existing Service Bus, telemetry, and caching wiring — to avoid duplicating what's already there.

The skill asks only for what it cannot safely infer.

## Database engine support

Both SQL Server and PostgreSQL are supported. The skill detects the engine from the existing codebase and applies the matching wiring throughout. If the engine is ambiguous it will ask before making changes.

## Reference

- [SKILL.md](./SKILL.md) — entry point, assumptions, and 6-step workflow summary.
- [references/workflow.md](./references/workflow.md) — detailed per-mode wiring instructions for both engines.
- [references/messaging-retrofit-checklist.md](./references/messaging-retrofit-checklist.md) — completion gate checklist.
- [references/messaging-retrofit-checkpoints.md](./references/messaging-retrofit-checkpoints.md) — inspection heuristics and detection signals.
