# Two-Phase Bootstrap Guide

This document guides you through the second phase of bootstrapping your CoreEx solution after the initial `dotnet new coreex` scaffold.

## Why Two-Phase Bootstrap?

During the initial scaffold (`dotnet new coreex`), reference-data and application-layer code doesn't yet exist. We intentionally:

- **Defer** CodeGen-dependent service registrations
- **Omit** the Application-layer `using` statements in the host projects
- **Provide** safe placeholder implementations

This ensures your scaffolded solution compiles immediately and lets you run the CodeGen tooling cleanly.

> **Hosts are added separately.** The base `coreex` scaffold creates the `Contracts`, `Application`, (optional) `Domain`, and `Infrastructure` projects plus the `*.Database` and `*.CodeGen` tooling. It does **not** create the API, Subscriber, or Outbox Relay hosts — those are added on demand with the `coreex-api`, `coreex-subscriber`, and `coreex-relay` templates. The host-wiring steps below (Steps 3–5) apply only to whichever hosts you have actually added.

## Phase 1: Initial Scaffold (Complete ✅)

Your solution is now scaffolded with:

```
✅ Contracts, Application, (optional) Domain, Infrastructure projects
✅ Database tooling (schema, migrations) — tools/app-name.Database
✅ Reference-data CodeGen tooling — tools/app-name.CodeGen
✅ Placeholder reference-data layer
```

**At this point:**
- The solution compiles cleanly
- No database schema is deployed yet
- Application services are stubs/placeholders

## Phase 2: Code Generation & Service Wiring (Start Here)

### Step 1: Apply the Database Schema (and generate persistence models)

From the database tooling project, run the composite `All` command (Create → Migrate → CodeGen → Schema → Data):

```bash
cd tools/app-name.Database
dotnet run -- All
```

**Result:**
- Database created (when it does not already exist)
- Migrations applied (schema + outbox tables)
- EF persistence models generated from the live schema
- Reference/seed data applied

> See `coreex-tooling.instructions.md` for the full DbEx command set (`All`, `Database`, `Migrate`, `CodeGen`, `Schema`, `Data`, `Reset`, …). Use the **commands** — not raw `--execute-*` flags.

### Step 2: Run CodeGen for Reference Data (contracts & services)

Generate the reference-data contracts, services, repository interfaces, repositories, and mappers:

```bash
cd tools/app-name.CodeGen
dotnet run
```

**Generated files appear in:**
- `src/app-name.Contracts/` (reference-data contract `*.g.cs`)
- `src/app-name.Application/` (service + repository-interface `*.g.cs`)
- `src/app-name.Infrastructure/Mapping/` (mapper `*.g.cs`)
- `src/app-name.Infrastructure/` (repository `*.g.cs`, persistence models)
- `src/app-name.Api/Controllers/` (`ReferenceDataController.g.cs` — once an API host exists)

> This is the **CoreEx** CodeGen (contracts etc. from `app-name.CodeGen/ref-data.yaml`), distinct from the **DbEx** CodeGen run by `All` in Step 1 (EF persistence models from `dbex.yaml` + live schema).

### Step 3: Enable Reference-Data Service Registration — API host *(if added)*

After CodeGen, uncomment the reference-data registrations in the API host.

#### In `src/app-name.Api/Program.cs`:

Find this commented block:

```csharp
        // NOTE: Reference-data orchestrator and dynamic service registration are performed AFTER CodeGen runs.
        // See: BOOTSTRAP_PHASE_2.md in your project root for the post-CodeGen setup steps.
        //
        // // #if refdata-enabled
        // builder.Services.AddReferenceDataOrchestrator<ReferenceDataService>();
        // builder.Services.AddDynamicServicesUsing<ReferenceDataService, ReferenceDataRepository>();
        // // #endif
```

**Uncomment** the two registrations (delete the NOTE):

```csharp
        builder.Services.AddReferenceDataOrchestrator<ReferenceDataService>();
        builder.Services.AddDynamicServicesUsing<ReferenceDataService, ReferenceDataRepository>();
```

> `AddDynamicServicesUsing<…>` takes **one representative type per assembly** (one Application type + one Infrastructure type registers every `[ScopedService]` in those assemblies) — do not add a type argument per service.

#### In `src/app-name.Api/GlobalUsing.cs`:

Find this commented block:

```csharp
// NOTE: Application layer using statements will be added after CodeGen runs.
// See: BOOTSTRAP_PHASE_2.md in your project root.
// // #if refdata-enabled
// global using app-name.Application;
// // #endif
```

**Uncomment** this line:

```csharp
global using app-name.Application;
```

### Step 4: Do the Same for the Subscriber Host *(if added)*

The Subscriber host is a full application-layer consumer, so it has the same registrations.

#### In `src/app-name.Subscriber/Program.cs`:

Uncomment:
```csharp
builder.Services
    .AddReferenceDataOrchestrator<ReferenceDataService>()
    .AddDynamicServicesUsing<ReferenceDataService, ReferenceDataRepository>();
```

#### In `src/app-name.Subscriber/GlobalUsing.cs`:

Uncomment:
```csharp
global using app-name.Application;
```

### Step 5: Outbox Relay Host — nothing to uncomment *(if added)*

The Outbox Relay host is a minimal publisher: it polls the outbox table and forwards events to the broker. It has **no application-layer dependencies** — no `AddReferenceDataOrchestrator`, no `AddDynamicServicesUsing`, no FusionCache, no EF `DbContext`, and no Application-layer `using`. There is **nothing to uncomment** for the Relay host.

### Step 6: Rebuild and Test

Now rebuild the entire solution:

```bash
dotnet build
dotnet test
```

**Expected result:**
- ✅ All projects compile
- ✅ All tests pass
- ✅ Reference-data services are wired correctly

### Step 7: Hand-Write Domain/Application Logic

With reference-data scaffolding complete, you can now:

1. **Write domain models** (if using DDD aggregates)
2. **Write application services** that use the generated reference-data
3. **Write business-logic validators**
4. **Implement subscriber handlers**

## Checklist for Phase 2

- [ ] Database schema deployed (`dotnet run -- All` in `tools/app-name.Database`)
- [ ] Reference-data CodeGen executed (`dotnet run` in `tools/app-name.CodeGen`)
- [ ] API host (if added): `Program.cs` reference-data registrations uncommented
- [ ] API host (if added): `GlobalUsing.cs` `global using app-name.Application;` uncommented
- [ ] Subscriber host (if added): `Program.cs` registrations + `GlobalUsing.cs` using uncommented
- [ ] Relay host (if added): nothing to do (no application-layer dependencies)
- [ ] Solution builds without errors
- [ ] Tests pass
- [ ] Reference-data endpoints responding at `/reference-data/*` (API host)

## Conditional Syntax Notes

If you scaffolded with `--refdata-enabled=false`:

- **No reference-data code was generated**
- **Leave the registrations commented out**
- **Don't uncomment the Application-layer using statements**
- The solution stays lightweight with just core infrastructure

If you scaffolded with `--refdata-enabled=true`:

- **Reference-data layer is fully generated**
- **Uncomment the registrations in Phase 2** (API and Subscriber hosts)
- **Application-layer using statements should be active**

## Troubleshooting

### "Type or namespace 'ReferenceDataService' could not be found"
→ You haven't uncommented the registrations in `Program.cs` yet. See Step 3 above.

### "The type or namespace name 'GenderCollection' could not be found" (Application project)
→ The Application project's `GlobalUsing.cs` must contain `global using app-name.Contracts;` so the generated repository/service `*.g.cs` can resolve the contract types. The template ships this; if missing, add it (do not fully-qualify the `.g.cs`).

### "Using statement was not recognized"
→ You haven't uncommented the Application-layer using statement in the host's `GlobalUsing.cs`. See Step 3.

### CodeGen reports "No entities defined"
→ Normal on first run. Add entities to your `app-name.CodeGen/ref-data.yaml` file and run CodeGen again.

## Next: Domain & Application Development

Once Phase 2 is complete, you're ready to:

- Design your domain models (if using DDD)
- Implement application services
- Add subscribers for async event handling
- Build out your API controllers

Refer to the CoreEx documentation, the `.github/instructions/*.instructions.md` files, and the samples for patterns on each layer.
