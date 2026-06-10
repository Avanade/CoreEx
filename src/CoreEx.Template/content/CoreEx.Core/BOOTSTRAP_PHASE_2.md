# Two-Phase Bootstrap Guide

This document guides you through the second phase of bootstrapping your CoreEx solution after the initial `dotnet new coreex` scaffold.

## Why Two-Phase Bootstrap?

During the initial scaffold (`dotnet new coreex`), reference-data and application-layer code doesn't yet exist. We intentionally:

- **Defer** CodeGen-dependent service registrations
- **Omit** Application layer `using` statements
- **Provide** safe placeholder implementations

This ensures your scaffolded solution compiles immediately and allows you to run the CodeGen tooling cleanly.

## Phase 1: Initial Scaffold (Complete ✅)

Your solution is now scaffolded with:

```
✅ API host (with placeholder services)
✅ Subscriber host (with PlaceholderSubscriber)
✅ Relay host (for outbox publishing)
✅ Database tooling (schema, migrations)
✅ Placeholder reference-data layer
```

**At this point:**
- The solution compiles cleanly
- No database schema is deployed yet
- Application services are stubs/placeholders

## Phase 2: Code Generation & Service Wiring (Start Here)

### Step 1: Run Database Migrations

Deploy the database schema:

```bash
cd tools/solution-name.Database
dotnet run -- "--execute-create-database-when-not-exists" "--execute-merge-migration"
```

**Result:**
- Schema created
- Outbox tables created
- Ready for reference-data seeding

### Step 2: Run CodeGen for Reference Data

Generate reference-data controllers, services, and repositories:

```bash
cd tools/solution-name.CodeGen
dotnet run
```

**Generated files appear in:**
- `src/solution-name.Application/` (Services, Validators)
- `src/solution-name.Infrastructure/Mapping/` (Mappers)
- `src/solution-name.Infrastructure/Persistence/` (Persistence models)
- `src/solution-name.Api/Controllers/` (ReferenceDataController.g.cs)

### Step 3: Enable Reference-Data Service Registration

After CodeGen, uncomment and enable the reference-data registrations.

#### In `src/solution-name.Api/Program.cs`:

Find this commented block:

```csharp
        // NOTE: Reference-data orchestrator and dynamic service registration are performed AFTER CodeGen runs.
        // See: BOOTSTRAP_PHASE_2.md in your project root for the post-CodeGen setup steps.
        // The following will be uncommented and moved here after running: dotnet run --project tools/solution-name.CodeGen
        //
        // // #if refdata-enabled
        // builder.Services.AddReferenceDataOrchestrator<ReferenceDataService>();
        // builder.Services.AddDynamicServicesUsing<ReferenceDataService, ReferenceDataRepository>();
        // // #endif
```

**Uncomment and move** these lines into the main service configuration:

```csharp
        // Add CoreEx services.
        builder.Services
            .AddExecutionContext()
            .AddMvcWebApi()
            .AddHttpWebApi()
            .AddReferenceDataOrchestrator<ReferenceDataService>()              // ← UNCOMMENTED
            .AddDynamicServicesUsing<ReferenceDataService, ReferenceDataRepository>();  // ← UNCOMMENTED
```

#### In `src/solution-name.Api/GlobalUsing.cs`:

Find this commented block:

```csharp
// NOTE: Application layer using statements will be added after CodeGen runs.
// See: BOOTSTRAP_PHASE_2.md in your project root.
// Add the following after generating application services:
// // #if refdata-enabled
// global using solution-name.Application;
// // #endif
```

**Uncomment** this line:

```csharp
global using solution-name.Application;  // ← UNCOMMENTED (remove conditional comment if refdata-enabled)
```

### Step 4: Do the Same for Subscriber Host

Repeat Step 3 for the Subscriber host:

#### In `src/solution-name.Subscriber/Program.cs`:

Uncomment:
```csharp
builder.Services
    .AddReferenceDataOrchestrator<ReferenceDataService>()
    .AddDynamicServicesUsing<ReferenceDataService, ReferenceDataRepository>();
```

#### In `src/solution-name.Subscriber/GlobalUsing.cs`:

Uncomment:
```csharp
global using solution-name.Application;
```

### Step 5: (Optional) Run Relay Host CodeGen

If you're using an Outbox Relay host:

#### In `src/solution-name.Outbox.Relay/Program.cs`:

Uncomment the reference-data registrations (if `refdata-enabled`).

#### In `src/solution-name.Outbox.Relay/GlobalUsing.cs`:

Uncomment the application layer `using` statement.

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

- [ ] Database schema deployed (run Database migrations)
- [ ] CodeGen executed (run CodeGen tool)
- [ ] API Program.cs reference-data registrations uncommented
- [ ] API GlobalUsing.cs application using statement uncommented
- [ ] Subscriber Program.cs reference-data registrations uncommented (if applicable)
- [ ] Subscriber GlobalUsing.cs application using statement uncommented (if applicable)
- [ ] Solution builds without errors
- [ ] Tests pass (if applicable)
- [ ] Reference-data endpoints responding at `/reference-data/*`

## Conditional Syntax Notes

If you scaffolded with `--refdata-enabled=false`:

- **No reference-data code was generated**
- **Leave the registrations commented out**
- **Don't uncomment the Application layer using statements**
- The solution stays lightweight with just core infrastructure

If you scaffolded with `--refdata-enabled=true`:

- **Reference-data layer is fully generated**
- **Uncomment all registrations in Phase 2**
- **Application layer using statements should be active**

## Troubleshooting

### "Type or namespace 'ReferenceDataService' could not be found"
→ You haven't uncommented the registrations in Program.cs yet. See Step 3 above.

### "Using statement was not recognized"
→ You haven't uncommented the Application layer using statement in GlobalUsing.cs. See Step 3.

### "The PlaceholderSubscriber doesn't seem right"
→ It's intentional! It's a compile-safe bootstrap placeholder. Replace it with your actual subscribers in Step 7.

### CodeGen reports "No entities defined"
→ Normal on first run. Add entities to your `ref-data.yaml` file and run CodeGen again.

## Next: Domain & Application Development

Once Phase 2 is complete, you're ready to:

- Design your domain models (if using DDD)
- Implement application services
- Add subscribers for async event handling
- Build out your API controllers

Refer to the CoreEx documentation and samples for patterns on each layer.
