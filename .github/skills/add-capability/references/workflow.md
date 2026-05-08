# Add Capability Workflow

## Step 0: Create and Approve Plan

Before making any changes to the domain, establish a durable plan that captures the domain state, proposed changes, and validation gates.

### 0.1 Load Context

Read these files to ground the plan in real repository conventions:

1. Instruction files in `/.github/instructions/`:
   - `host-setup.instructions.md`
   - `event-subscribers.instructions.md`
   - `application-services.instructions.md`
   - `database-project.instructions.md`

2. Sample host wiring from:
   - `samples/src/Contoso.Products.Api/Program.cs`
   - `samples/src/Contoso.Products.Subscribe/Program.cs`
   - `samples/src/Contoso.Products.Outbox.Relay/Program.cs`

3. Domain templates under `/.github/templates/domain/**`

4. Review `.agent/execplans/` to understand plan structure

### 0.2 Inspect Domain State

Determine the current shape before proposing changes.

Inspect for:
- Domain boundary and project names
- Existing hosts: `*.Api`, `*.Outbox.Relay`, `*.Subscribe`
- Database support: `*.Database` project, outbox tables/procedures, SQL Server references
- Messaging support: `CoreEx.Events`, `CoreEx.Azure.Messaging.ServiceBus`, `AddEventFormatter`, `AddSqlServerOutboxPublisher`, `AddSubscribedManager`, `AzureServiceBusReceiving`
- Existing telemetry and health wiring
- Integration-event semantics: subjects, subscriber classes, related service methods

Use conservative detection. Ask if ambiguous.

### 0.3 Clarify User Intent

Ask only what cannot be inferred:
- Which domain to retrofit?
- Which capability: relay, subscribe, subscriber classes, or combined?
- Use SQL Server and Azure Service Bus as defaults?
- If adding subscribers: what subjects and payload contracts?
- Infrastructure/host wiring only, or also application-facing handlers?

### 0.4 Create Capability-Addition Plan

Scaffold `.agent/execplans/capability-{solution}-{domain}-{capability}.md` using the template at `/.github/skills/coreex-exec-plan/assets/templates/PLAN.template.md`.

Fill these sections:
- **Purpose / Big Picture**: What capability is being added? How will the domain behave differently?
- **Context and Orientation**: Current domain shape, existing hosts, packages, databases, event subjects.
- **Plan of Work**: Prose description of which hosts will be modified, which projects created, which registrations added.
- **Concrete Steps**: Build/test commands for validation.
- **Validation and Acceptance**: How to verify the capability works (e.g., outbox events written, subscribers consume messages).
- **Interfaces and Dependencies**: New packages, service references, event payload contracts.
- **Progress**: Checklist section (leave empty until execution).

### 0.5 Choose Retrofit Mode

Document the choice in the plan:

#### Mode A — Add Outbox.Relay Only
Use when domain already writes data and should publish integration events reliably.

Expected work:
- Create `*.Outbox.Relay` project if missing
- Add packages and project references
- Add relay `Program.cs` wiring per host-setup conventions
- Ensure database has outbox tables and procedures
- Ensure API host has event formatter + outbox publisher wiring

#### Mode B — Add Subscribe Only
Use when domain must consume integration events/commands from other services.

Expected work:
- Create `*.Subscribe` project if missing
- Add Service Bus client and receiver wiring
- Add hosted service manager and mapping
- Add subscriber classes and registration
- Reuse reference data, cache, infrastructure, telemetry patterns

#### Mode C — Add Both Relay and Subscribe
Service publishes its own events AND consumes events from others.

#### Mode D — Add Subscribers to Existing Subscribe Host
Host exists but subscriber classes, registration, or error handling incomplete.

### 0.6 Update PLANS Index

Add an entry to `.agent/PLANS.md`:
- Title: "Add {capability} capability to {Solution}.{Domain}"
- Purpose: Brief description of what the capability enables
- Status: `Pending approval`
- Mode: A, B, C, or D
- Created: Today's date

### 0.7 Approval Checkpoint

Present the plan to the user. **Do not proceed to Step 1 until user approves.**

Once approved, update `.agent/PLANS.md` status to `In progress` and begin Step 1.

---

## Step 1: Load Context

Before making changes, load:

1. Instruction files in `/.github/instructions/`:
   - `host-setup.instructions.md`
   - `event-subscribers.instructions.md`
   - `application-services.instructions.md`
   - `database-project.instructions.md`

2. Sample host wiring from:
   - `samples/src/Contoso.Products.Api/Program.cs`
   - `samples/src/Contoso.Products.Subscribe/Program.cs`
   - `samples/src/Contoso.Products.Outbox.Relay/Program.cs`

3. Domain templates under `/.github/templates/domain/**`

## Step 2: Apply Incremental Changes

Prefer targeted edits over regeneration.

Rules:
1. Reuse existing project naming and layering
2. Do not duplicate wiring that exists
3. Keep subscriber logic thin; delegate to Application services
4. Preserve host middleware order and telemetry conventions
5. Reuse domain templates only for missing pieces
6. If domain shape inconsistent, stop and explain blockers

Apply changes based on the retrofit mode chosen in Step 0.5:

### Mode A Changes — Add Outbox.Relay

1. Create `{Solution}.{Domain}.Outbox.Relay` project structure
2. Update `{Solution}.{Domain}.Api/Program.cs`:
   - Add `AddEventFormatter`
   - Add `AddSqlServerOutboxPublisher`
3. Ensure `{Solution}.{Domain}.Database`:
   - Contains outbox tables and stored procedures
   - See `samples/src/Contoso.Products.Database` for reference
4. Create `{Solution}.{Domain}.Outbox.Relay/Program.cs` with relay host setup per conventions

### Mode B Changes — Add Subscribe

1. Create `{Solution}.{Domain}.Subscribe` project structure
2. Update or create subscriber classes under `{Domain}.Subscribe/{TopicName}Subscriber.cs`
3. Update `{Solution}.{Domain}.Subscribe/Program.cs`:
   - Add `AddSubscribedManager`
   - Register subscribers
   - Add Azure Service Bus receiver wiring
4. Add project references as needed

### Mode C Changes — Add Both

Apply both Mode A and Mode B changes.

### Mode D Changes — Enhance Existing Subscribe Host

1. Add new subscriber classes to `{Domain}.Subscribe`
2. Update `Program.cs` registration if needed
3. Add or refine error handling and logging

## Step 3: Build and Preliminary Validation

Run:

   Command: `dotnet build CoreEx.sln`
   Expected result: Clean build with no warnings.

## Step 4: Validate

Run messaging-retrofit-checklist.md completion gate.

Minimum criteria:
- `Program.cs` files follow host setup conventions
- Required package/project references present
- Relay: outbox database assets exist when relay added
- Subscribe: Subscribers registered with `SubscribedBase` patterns
- Files fit existing naming/layering conventions

## Step 5: Test (If Applicable)

If test projects exist for the domain:

   Command: `dotnet test tests/{Solution}.{Domain}.Test.Api`
   Expected result: All tests pass.

   Command: `dotnet test tests/{Solution}.{Domain}.Test.Subscribe`
   Expected result: All tests pass (if subscribe tests exist).

## Step 6: Document Changes

Capture what was added/modified in the plan file.

## Step 7: Update Plan and Mark Complete

Update `.agent/execplans/capability-{solution}-{domain}-{capability}.md`:
- Move each completed work item to `Progress` with timestamp
- Record any surprises in `Surprises & Discoveries`
- Update `Outcomes & Retrospective` with proof of success

Update `.agent/PLANS.md`:
- Change status to `Completed`
- Add completion date
- Add brief summary of capabilities added
