# Generate Domain

Scaffolds all layers of a new CoreEx domain from scratch using reasoning and convention — asking for your entity shape and generating code tailored to it, aligned to the Contoso sample architecture.

## When to use

| Scenario | Use this | Not this |
|----------|----------|----------|
| Entity has custom fields, types, or business rules | `/generate-domain` | — |
| You want the agent to reason about validation, event naming, and query config | `/generate-domain` | — |
| Entity fits the standard template shape exactly and you want fast, exact output | — | `/scaffold-domain-from-templates` |
| Adding capabilities to an existing domain | — | `/add-capability` |

## How to invoke

**Claude Code:**
```
/generate-domain
```

**GitHub Copilot Chat:**
```
#file:.github/skills/generate-domain/SKILL.md  scaffold a new Orders domain with an Order entity
```

Optionally supply solution, domain, and entity up front — e.g. `Contoso Orders Order`. The skill will confirm all inputs before generating anything.

## What gets generated

All layers in order, with baseline test projects:

| Layer | Project |
|-------|---------|
| Contracts | `{Solution}.{Domain}.Contracts` — entity contracts, reference data types |
| Application | `{Solution}.{Domain}.Application` — services, interfaces, validators |
| Infrastructure | `{Solution}.{Domain}.Infrastructure` — EF Core repositories, mappers, persistence models |
| API | `{Solution}.{Domain}.Api` — controllers, `Program.cs`, OpenAPI |
| Database | `{Solution}.{Domain}.Database` — migrations, `dbex.yaml`, seed data |
| Unit tests | `{Solution}.{Domain}.Test.Unit` |
| API tests | `{Solution}.{Domain}.Test.Api` |

The generated code follows CoreEx conventions throughout: `[Contract]`, `IETag`, `IChangeLog`, `[IdempotencyKey]`, `TransactionAsync`, `QueryArgsConfig`, outbox events, FusionCache, OpenTelemetry.

## Reference

- [SKILL.md](./SKILL.md) — entry point, required inputs, and workflow overview.
- [references/workflow.md](./references/workflow.md) — detailed 8-phase generation workflow with naming conventions and quality gates.
- [Domain scaffold templates](../../templates/domain/README.md) — use instead when the entity fits the standard shape and you want deterministic output.
