# Domain Scaffold Templates

Ready-made templates for bootstrapping a new CoreEx domain. Run the scaffold prompt, answer a few questions, and get a compilable, test-covered project set — with no hand-written generated files.

## How to use

**Claude Code** — open this repo and run:

```
/scaffold-domain-from-templates
```

**GitHub Copilot Chat** (VS Code) — open Copilot Chat and attach the prompt file:

```
#file:.github/prompts/scaffold-domain-from-templates.prompt.md  scaffold a new domain
```

In both cases the agent will ask you the questions below, then clone and materialise every relevant template with your values substituted in. No files are generated that would otherwise come from a code generator (no `*.g.cs`, `*.g.sql`, etc.).

## Questions you'll be asked

1. **Solution** and **Domain** names (e.g. `Contoso` / `Orders`).
2. **Entity** name (e.g. `Order`) — plural, kebab, and snake-case variants are derived automatically.
3. **Database engine** — SQL Server (default) or PostgreSQL.
4. **Reference Data** — whether the domain has a status/lookup type. Yes includes a `CodeGen` project that generates contracts, mappers, and endpoints via `CoreEx.CodeGen`; No removes all ref-data patterns entirely. Default **No**.
5. **Child Entity** — whether the primary entity owns a child collection (e.g. `OrderItem`). Conditional migration and infrastructure wiring included. Default **No**.
6. **Domain project** — whether to include a DDD-style domain layer with aggregate roots and value objects. Default **No**.
7. **ROP** (Railway Oriented Programming) — whether service and repository layers use `Result<T>` pipelines instead of throw-on-failure. Default **No**.
8. **Outbox Relay** — whether to include a standalone relay host that forwards outbox events to Service Bus. Default **Yes**.
9. **Subscribe** — whether to include an event-subscriber host. Default **Yes**.

## Directory layout

```
templates/domain/
├── Contracts/              # Shared entity contracts
├── Application/            # Services, interfaces, validators
│   └── rop/                # Result<T> variants (ROP = Yes)
├── Infrastructure/
│   ├── _shared/            # Engine-agnostic repositories, mappers, persistence models
│   │   └── rop/            # Result<T> variants (ROP = Yes)
│   ├── sqlserver/          # SQL Server DbContext and csproj
│   └── postgres/           # PostgreSQL DbContext and csproj
├── Api/
│   ├── sqlserver/          # SQL Server Program.cs and csproj
│   └── postgres/           # PostgreSQL Program.cs and csproj
├── Database/
│   ├── _shared/            # Engine-agnostic seed data
│   ├── sqlserver/          # SQL Server migrations and DbEx config
│   └── postgres/           # PostgreSQL migrations and DbEx config
├── CodeGen/                # Reference data code-gen console (Ref Data = Yes)
├── Domain/                 # DDD aggregate roots and value objects (Domain = Yes)
├── Outbox.Relay/
│   ├── sqlserver/          # SQL Server relay host (Outbox Relay = Yes)
│   └── postgres/           # PostgreSQL relay host (Outbox Relay = Yes)
└── Subscribe/
    ├── _shared/            # Subscriber class and global usings
    ├── sqlserver/          # SQL Server subscriber host (Subscribe = Yes)
    └── postgres/           # PostgreSQL subscriber host (Subscribe = Yes)
```

`_shared/` directories are always included. Engine-specific directories are selected based on your database engine answer; the other is dropped.

## Reference

- **[scaffold-domain-from-templates.prompt.md](../../prompts/scaffold-domain-from-templates.prompt.md)** — full placeholder list, conditional inclusion rules, and post-generation steps the agent follows.
- **[DomainScaffold.checklist.md](./DomainScaffold.checklist.md)** — acceptance checklist the agent runs through before reporting completion.
