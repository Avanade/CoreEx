# coreex-aggregate: Workflow

Full workflow for adding or modifying a DDD domain object (aggregate root, entity, or value object)
in a CoreEx `*.Domain` project.

---

## Phase 0 — Confirm the Domain Layer Applies

Before creating anything, confirm a Domain layer is warranted at all:

| Question | Guidance |
|---|---|
| Does this concept have invariants that must be enforced at the model level (state machines, child-collection rules, cross-property constraints)? | If no → skip the Domain layer; let the Application service orchestrate directly against repository interfaces. |
| Does the domain already have a `*.Domain` project? | If no and the answer above is yes, the solution needs `--domain-driven-enabled true` — this is a solution-scaffolding concern, not something this skill creates. Confirm with the developer before proceeding. |
| Is this a CRUD-oriented entity (simple get/create/update/delete, no business rules beyond validation)? | If yes → skip the Domain layer entirely; use `coreex-contract` + `coreex-repository` + `coreex-app-service` directly. |

Only proceed past this point when a Domain layer is confirmed to exist and be warranted.

---

## Phase 1 — Interview: Which Domain Object?

| Question | Branch |
|---|---|
| Is this the consistency boundary itself — the thing loaded, mutated, and saved as a unit, potentially raising integration events? | **Aggregate Root** → Step 1 |
| Is this owned exclusively by an aggregate, has its own identity, but has no independent lifecycle outside the aggregate? | **Entity** → Step 2 |
| Does this concept have no identity — it's fully defined by its values (a price, a range, a measurement)? | **Value Object** → Step 3 |

A single feature request often needs more than one — e.g., an aggregate root with one or more child
entities and a value object used by both. Work through each in the order above; child entities and
value objects are usually needed to complete the aggregate root's design.

---

## Step 1 — Aggregate Root

Extend `Aggregate<TId, TSelf>` from `CoreEx.DomainDriven`. This is the consistency boundary — the
only object the Application layer loads, mutates, and persists directly.

```csharp
// Domain/{Aggregate}.cs
namespace {Domain}.Domain;

public sealed class {Aggregate} : Aggregate<{IdType}, {Aggregate}>
{
    // Backing field for a child-entity collection (if applicable) — never expose the mutable list.
    private List<{ChildEntity}> _items = [];

    /// <summary>Creates a new {Aggregate}.</summary>
    public static {Aggregate} CreateNew({CtorArgs}) => new {Aggregate}(Runtime.NewId())
    {
        // Set initial state for a brand-new instance.
        {Property} = {value}
    }.AsNew();

    /// <summary>Reconstructs an existing {Aggregate} from persisted data.</summary>
    public static {Aggregate} CreateFrom({IdType} id, {CtorArgs}, IEnumerable<{ChildEntity}>? items, ChangeLog? changeLog, string? etag) => new {Aggregate}(id)
    {
        {Property} = {value},
        _items = items is null ? [] : [.. items.Select(i => i.Clone(PersistenceState.NotModified))],
        ChangeLog = changeLog,
        ETag = etag
    }.AsNotModified();

    // Constructor is private — CreateNew/CreateFrom are the only public construction paths.
    private {Aggregate}({IdType} id) : base(id) { }

    public IReadOnlyList<{ChildEntity}> Items => _items;

    /// <summary>Enforces the mutation guard — called automatically by every Modify/Remove.</summary>
    protected override Result OnCheckCanMutate() => {StatusProperty}.CanBeMutated
        ? Result.Success
        : Result.BusinessError($"{Aggregate} has a status of '{StatusProperty}' and cannot be modified.",
            c => c.WithKey(Id).WithErrorCode("invalid-status"));

    /// <summary>Re-derives dependent state after every successful mutation.</summary>
    protected override void OnMutate()
    {
        // Recalculate anything that depends on current child-entity/property state.
    }

    /// <summary>Adds a new {ChildEntity} to the aggregate.</summary>
    public Result {ChildEntity}Add({ChildEntity} item) => Modify(() =>
    {
        item.ThrowIfNull();
        _items.Add(item.Clone(PersistenceState.New));
        return Result.Success;
    });
}
```

**Rules:**
- `CreateNew(...)` calls `.AsNew()`; `CreateFrom(...)` calls `.AsNotModified()` — these are the **only**
  two public construction paths. The constructor is `private`.
- Only expose **read-only** properties and a small set of intention-revealing **mutation methods** —
  never a public setter that bypasses `Modify`.
- `OnCheckCanMutate()` returns `Result.Success` or `Result.BusinessError(...)` — this is invoked via
  `CheckCanMutate()` → `.ThrowOnError()` internally, so a failing guard **throws the exception carried
  by the failed `Result`** when `Modify`/`Remove` is called — typically a `BusinessException` when
  `Result.BusinessError(...)` is used, but whatever exception type the `Result` actually carries.
- `OnMutate()` is called automatically after every successful `Modify`/`Remove` — use it to re-derive
  dependent state (e.g., recompute a status from child-entity state), not to perform I/O.
- Public mutation methods (e.g., `{ChildEntity}Add`) should return `Result`/`Result<T>` for consistency
  with Application-layer `Result<T>` pipelines — this makes expected business failures explicit and
  composable, even though the internal guard mechanism throws. Throwing `BusinessException` directly
  from a mutation method is acceptable where it reads more naturally, but `Result` is the recommended
  default.
- Add integration events with the inherited `AddEvent(EventData)` — these are **integration** events
  only (to inform other systems), never domain events (in-process notifications); see
  [Integration Events](#integration-events-not-domain-events) below.
- Never perform async I/O (repository calls, HTTP requests, adapter calls) inside the aggregate —
  that belongs in Application services or Policies.

---

## Step 2 — Child Entity

Extend `Entity<TId, TSelf>`. A child entity has its own identity but no independent lifecycle outside
the owning aggregate. Its `CreateNew`/`CreateFrom` factory methods are `public` (the aggregate, or
occasionally the Application layer, may construct an instance to pass into the aggregate), but every
**mutation method** is `internal` — once inside the aggregate, only the aggregate may change it.

```csharp
// Domain/{ChildEntity}.cs
namespace {Domain}.Domain;

public sealed class {ChildEntity} : Entity<{IdType}, {ChildEntity}>
{
    /// <summary>Creates a new {ChildEntity}.</summary>
    public static {ChildEntity} CreateNew({CtorArgs})
        => new {ChildEntity}(Runtime.NewId()) { {Property} = {value} }.AsNew();

    /// <summary>Reconstructs an existing {ChildEntity} from persisted data.</summary>
    public static {ChildEntity} CreateFrom({IdType} id, {CtorArgs}, string? etag)
        => new {ChildEntity}(id) { {Property} = {value}, ETag = etag }.AsNotModified();

    private {ChildEntity}({IdType} id) : base(id) { }

    public {PropertyType} {Property} { get; private set => field = value.ThrowIfNull(); } = null!;

    // Mutation methods are internal — only the owning aggregate may invoke them.
    internal void Override{Property}({PropertyType} value) => Modify(() => {Property} = value);
    internal void Delete() => Remove();

    // Optional consumer-authored helper — NOT provided by CoreEx.DomainDriven. Useful when the owning
    // aggregate's CreateFrom needs to re-tag a rehydrated child collection with a specific PersistenceState.
    internal {ChildEntity} Clone(PersistenceState state) => CreateFrom(Id, {CtorArgs}, ETag).SetPersistenceState(state);
}
```

**Rules:**
- Same factory-method + private-constructor pattern as the aggregate root: `CreateNew` / `CreateFrom`,
  `.AsNew()` / `.AsNotModified()`.
- Mutation methods are **`internal`**, never `public` — this ensures only the owning aggregate can
  drive a child entity's mutations once it has been added to the aggregate.
- Identity-based equality — `Entity<TId, TSelf>.Equals` only compares `Id`, not property values.
- `Clone(PersistenceState)` (shown above) is **not** a `CoreEx.DomainDriven` base-class member — it is
  a common consumer-authored pattern, calling the entity's own `CreateFrom` plus the inherited
  `protected SetPersistenceState(...)`, used when the aggregate's `CreateFrom` reconstructs its child
  collection and needs to re-tag each item as `NotModified` (or `New`, when merging a newly added item).

---

## Step 3 — Value Object

Implement as a `sealed record` — no identity, defined entirely by its values, structural equality and
`with`-expression mutation for free. Enforce invariants in the initialiser.

```csharp
// Domain/ValueObjects/{ValueObject}.cs
namespace {Domain}.Domain.ValueObjects;

public sealed record class {ValueObject}
{
    public required {PropertyType} {Property} { get; init => field = value.ThrowIfLessThanZero(); }
    public {OtherType} {OtherProperty} { get; init => field = value.ThrowIfNull(); }

    // Derived read-only members are welcome — they are not persisted, only computed.
    public {ResultType} {Derived} => {expression};

    /// <summary>Validates cross-property invariants not expressible via a single property initialiser.</summary>
    public {ValueObject} EnsureIsValid() => {condition} ? this
        : throw new ValidationException("{Description of the violated invariant}.");
}
```

**Rules:**
- `sealed record`, never a mutable class — structural equality and `with`-expression semantics are
  intrinsic to a value object's definition.
- Enforce single-property invariants directly in the `init` accessor using the guard helpers
  (`.ThrowIfNull()`, `.ThrowIfLessThanZero()`, `.ThrowIfInactive()`, etc.) — they return the guarded
  value, making them composable inline.
- Cross-property invariants that cannot be expressed in a single `init` accessor go in an
  `EnsureIsValid()` method, called by the owning entity/aggregate after construction.
- Place value objects in a `ValueObjects/` sub-folder within the Domain project.
- A value object never has a factory method pair (`CreateNew`/`CreateFrom`) — it is constructed
  directly with object initializer syntax; there is no `PersistenceState` to track because it has
  no independent identity.

---

## Guard Helpers Reference

These extension methods return the guarded value on success (enabling inline chaining) and throw on
failure — used throughout property setters, `init` accessors, and factory method bodies:

| Helper | Throws when |
|---|---|
| `.ThrowIfNull()` | value is `null` |
| `.ThrowIfNullOrEmpty()` | value is `null` or an empty string |
| `.ThrowIfInactive()` | a reference-data value's `IsActive` is `false` |
| `.ThrowIfLessThanZero()` | a numeric value is less than zero |

Chain them for multiple checks on one value: `value.ThrowIfNull().ThrowIfInactive()`.

---

## PersistenceState Reference

| State | Meaning | Set by |
|---|---|---|
| `Unknown` | Default; never a valid target state — `SetPersistenceState` throws if asked to set it | — |
| `New` | Newly created; insert on next commit | `.AsNew()` in `CreateNew(...)` |
| `NotModified` | Loaded from store; no action required | `.AsNotModified()` in `CreateFrom(...)` — this transition is only permitted from `Unknown` |
| `Modified` | Changed since load; update on next commit | Automatically by `Modify(...)`, but **only** when currently `NotModified` — a `New` entity that is modified stays `New` |
| `Removed` | Marked for deletion; delete on next commit | Automatically by `Remove(...)` — this is terminal: `Remove(...)` also calls `MakeReadOnly()`, so a removed entity cannot be mutated further |

Filter helpers for child-entity collections:

```csharp
_items.Where(i => i.PersistenceState.IsNotRemoved)     // active items only
_items.Any(i => i.PersistenceState.IsNewOrModified)     // has-changes check
```

---

## Integration Events (Not Domain Events)

`Aggregate<TId, TSelf>` provides `AddEvent(EventData)` and `ClearEvents()` for **integration events**
only — coarse-grained notifications to other systems, published via the transactional outbox from the
Application layer. CoreEx deliberately does not provide in-process domain-event dispatch (no MediatR
`INotification`-style mechanism):

- **Why:** fine-grained domain events generate high event volumes with implicit, hard-to-trace
  side-effects; coarse integration events communicated through `IUnitOfWork.Events` are explicit,
  auditable, and transactional.
- Add events from within a mutation method using the inherited `AddEvent(...)`.
- The Application service is responsible for forwarding `aggregate.Events` into `_unitOfWork.Events`
  inside the same `TransactionAsync(...)` scope that persists the aggregate — see
  `.github/instructions/coreex-application-services.instructions.md` for the Application-layer side
  of this handoff.
- If a genuine in-process domain-event use case exists, that is an opt-in extension a consumer adds
  themselves (e.g., raising events dispatched via MediatR after commit) — not a CoreEx default.

---

## Guardrails

- **No async I/O in domain classes** — repository calls, HTTP requests, adapter calls all belong in
  Application services or Policies, never in an aggregate, entity, or value object
- **Constructor is always `private`** — `CreateNew(...)` and `CreateFrom(...)` are the only public
  construction paths for aggregates and entities
- **Child entity mutation methods are `internal`**, never `public` — only the owning aggregate may
  invoke them; the Application layer never mutates a child entity directly
- **`Modify(...)`/`Remove(...)` are the only mutation paths** — never mutate a private field directly
  outside of one of these wrappers; they enforce `OnCheckCanMutate()`, transition `PersistenceState`,
  and call `OnMutate()` consistently
- **`Remove(...)` is terminal** — it sets `PersistenceState.Removed` and then calls `MakeReadOnly()`;
  there is no built-in restore path, so any further `Modify`/`Remove` call on that instance throws
- **Prefer `Result`/`Result<T>` returns from public mutation methods** — even though `OnCheckCanMutate()`
  failures always throw internally, the public method signature staying `Result`-based keeps it
  composable in Application-layer `Result<T>` pipelines; throwing `BusinessException` directly is
  acceptable but not the default recommendation
- **Value objects are `sealed record`, never mutable classes** — `init`-only properties with invariant
  enforcement in the accessor or an `EnsureIsValid()` method
- **No native domain-event dispatch** — only integration events via `Aggregate<TId,TSelf>.Events`,
  forwarded by the Application layer; do not introduce MediatR or an in-process event bus into the
  Domain layer
- **Do not reference Infrastructure, Application, or host assemblies** from the Domain layer — it
  depends only on `Contracts` and `CoreEx`/`CoreEx.DomainDriven`
- **Skip the Domain layer entirely for CRUD-oriented entities** — introducing aggregates/entities for
  a domain with no real invariants adds ceremony without benefit; use the Application layer directly
  against repository interfaces instead
