# Add Capability Workflow

## Step 1: Load Context

Before making changes, load:

1. Instruction files in `/.github/instructions/`:
   - `coreex-host-setup.instructions.md`
   - `coreex-event-subscribers.instructions.md`
   - `coreex-application-services.instructions.md`
   - `coreex-tooling.instructions.md`

2. Sample host wiring from:
   - `samples/src/Contoso.Products.Api/Program.cs`
   - `samples/src/Contoso.Products.Subscribe/Program.cs`
   - `samples/src/Contoso.Products.Outbox.Relay/Program.cs`

3. Domain templates under `/.github/templates/domain/**`

## Step 2: Inspect Domain State

Determine current shape before proposing changes.

Inspect for:
- Domain boundary and project names
- Existing hosts: `*.Api`, `*.Outbox.Relay`, `*.Subscribe`
- Database support: `*.Database` project, outbox tables/procedures, SQL Server references
- Messaging support: `CoreEx.Events`, `CoreEx.Azure.Messaging.ServiceBus`, `AddEventFormatter`, `AddSqlServerOutboxPublisher`, `AddSubscribedManager`, `AzureServiceBusReceiving`
- Existing telemetry and health wiring
- Integration-event semantics: subjects, subscriber classes, related service methods

Use conservative detection. Ask if ambiguous.

## Step 3: Clarify User Intent

Ask only what cannot be inferred:
- Which domain to retrofit?
- Which capability: relay, subscribe, subscriber classes, or combined?
- Use SQL Server and Azure Service Bus as defaults?
- If adding subscribers: what subjects and payload contracts?
- Infrastructure/host wiring only, or also application-facing handlers?

## Step 4: Choose Retrofit Mode

### Mode A — Add Outbox.Relay
Use when domain already writes data and should publish integration events reliably.

Expected work:
- Create `*.Outbox.Relay` project if missing
- Add packages and project references
- Add relay `Program.cs` wiring per host-setup conventions
- Ensure database has outbox tables and procedures
- Ensure API host has event formatter + outbox publisher wiring

### Mode B — Add Subscribe
Use when domain must consume integration events/commands from other services.

Expected work:
- Create `*.Subscribe` project if missing
- Add Service Bus client and receiver wiring
- Add hosted service manager and mapping
- Add subscriber classes and registration
- Reuse reference data, cache, infrastructure, telemetry patterns

### Mode C — Add Both Relay and Subscribe
Service publishes its own events AND consumes events from others.

### Mode D — Add Subscribers to Existing Subscribe Host
Host exists but subscriber classes, registration, or error handling incomplete.

## Step 5: Apply Incremental Changes

Prefer targeted edits over regeneration.

Rules:
1. Reuse existing project naming and layering
2. Do not duplicate wiring that exists
3. Keep subscriber logic thin; delegate to Application services
4. Preserve host middleware order and telemetry conventions
5. Reuse domain templates only for missing pieces
6. If domain shape inconsistent, stop and explain blockers

## Step 6: Validate

Run messaging-retrofit-checklist.md completion gate.

Minimum criteria:
- `Program.cs` files follow host setup conventions
- Required package/project references present
- Relay outbox database assets exist when relay added
- Subscribers registered with `SubscribedBase` patterns
- Files fit existing naming/layering conventions
- Clean build/diagnostics
