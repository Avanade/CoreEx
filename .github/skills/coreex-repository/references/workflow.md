# coreex-repository: Workflow

Full workflow for creating or modifying a CoreEx Infrastructure repository in `Infrastructure/Repositories/`. Follow the path that matches the request.

---

## Phase 1 — Clarify Before Writing

Answer these questions before emitting any code.

| Question | Default | Notes |
|---|---|---|
| Which entity and database type? | Ask | PostgreSQL: `PostgresDatabase` + `UseNpgsql`; SQL Server: `SqlServerDatabase` + `UseSqlServer` — check the project's `Program.cs` |
| New repository or adding to an existing one? | Ask | New → Path A (scaffold); Existing → Path B/C/D |
| Operations needed? | Ask | Get / Create / Update / Delete / Query |
| Using `Result<T>` / ROP pipelines? | No | Yes → use `*WithResultAsync` + `Result<T>` pipeline (Path D); either style works at the repository level — this is a per-project or per-service choice |
| Dynamic filtering/ordering on query? | No | Yes → `QueryArgsConfig` (Path C) |

---

## Path A — New Repository Class

### A1 — Scaffold repository

```csharp
namespace {Solution}.Infrastructure.Repositories;

[ScopedService<I{Name}Repository>]
public class {Name}Repository({Solution}EfDb ef) : I{Name}Repository
{
    private readonly {Solution}EfDb _ef = ef.ThrowIfNull();

    public Task<Contracts.{Name}?> GetAsync(string id) => _ef.{Name}s.GetAsync(id);

    public Task<DataResult<Contracts.{Name}>> CreateAsync(Contracts.{Name} value) => _ef.{Name}s.CreateAsync(value);

    public Task<DataResult<Contracts.{Name}>> UpdateAsync(Contracts.{Name} value) => _ef.{Name}s.UpdateAsync(value);

    public Task<DataResult> DeleteAsync(string id) => _ef.{Name}s.DeleteAsync(id);
}
```

`[ScopedService<I{Name}Repository>]` auto-registers via `AddDynamicServicesUsing<T>()` — no manual DI wiring needed.

### A2 — Add EfDb accessor

In `*EfDb.cs`, add a strongly-typed property for the new entity. Use `EfDbMappedModel` when the mapper already exists; `EfDbModel` for plain models (ref-data, read-only joins):

```csharp
// Contract ↔ Persistence (has a BiDirectionMapper):
public EfDbMappedModel<Contracts.{Name}, Persistence.{Name}, {Name}Mapper> {Name}s
    => Model<Persistence.{Name}>().ToMappedModel<Contracts.{Name}, {Name}Mapper>({Name}Mapper.Default);

// Plain model (no contract mapping needed):
public EfDbModel<Persistence.{Name}> {Name}s => Model<Persistence.{Name}>();
```

Add a `WithModel<Persistence.{Name}>()` entry in `EfDbOptions` if the entity needs a logical-delete filter or other per-model configuration:

```csharp
private static readonly EfDbOptions _options = new EfDbOptions()
    .WithModel<Persistence.{Name}>(m => m.WithLogicalDeleteFilter());
```

### A3 — Add BiDirectionMapper

Create `Infrastructure/Mapping/{Name}Mapper.cs`. Override **both** `OnMap` overloads. Map `Id` and all domain-specific properties explicitly. **Do not** map `ETag` or `ChangeLog` — the base mapper handles them:

```csharp
namespace {Solution}.Infrastructure.Mapping;

public class {Name}Mapper : BiDirectionMapper<Contracts.{Name}, Persistence.{Name}, {Name}Mapper>
{
    protected override Persistence.{Name} OnMap(Contracts.{Name} source) => new()
    {
        Id = source.Id!,
        Text = source.Text!,
        StatusCode = source.Status?.Code!
        // map all domain fields; leave out ETag and ChangeLog
    };

    protected override Contracts.{Name} OnMap(Persistence.{Name} source) => new()
    {
        Id = source.Id,
        Text = source.Text,
        StatusCode = source.StatusCode
        // map all domain fields; leave out ETag and ChangeLog
    };
}
```

**`OnMap` — not `OnMapToPrimary` / `OnMapToSecondary`.** Two overloads, same name, different source type.

**`Id` is always mapped explicitly** — the base does not auto-map it.

**Never map `ETag` or `ChangeLog`** — the base `BiDirectionMapper` owns those. The persistence model exposes `ETag` (a `string?`) — there is no `RowVersion` member.

Ensure `global using {Solution}.Infrastructure.Mapping;` is in `GlobalUsing.cs` so other classes can reference `{Name}Mapper.Default` without a fully-qualified name.

### A4 — DbContext (new domain only)

If setting up a completely new domain, the `*DbContext` is a partial class that ties EF Core to `IDatabase`:

```csharp
namespace {Solution}.Infrastructure.Repositories;

// PostgreSQL: replace PostgresDatabase → SqlServerDatabase and UseNpgsql → UseSqlServer for SQL Server
public partial class {Solution}DbContext(DbContextOptions<{Solution}DbContext> options, PostgresDatabase database)
    : DbContext(options), IEfDbContext
{
    public IDatabase BaseDatabase { get; } = database.ThrowIfNull();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        if (!optionsBuilder.IsConfigured)
            optionsBuilder.UseNpgsql(BaseDatabase.Connection, contextOwnsConnection: false);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) => AddGeneratedModels(modelBuilder);

    partial void AddGeneratedModels(ModelBuilder modelBuilder);
}
```

`AddGeneratedModels` is implemented in the generated `*DbContext.g.cs` — the partial method declaration compiles as a no-op until CodeGen has run.

---

## Path B — Add a CRUD Operation to an Existing Repository

Locate the repository class and add the method using the EfDb delegate shortcuts. No raw `DbContext` queries for simple CRUD:

```csharp
// Get by key — returns null when not found; service checks
public Task<Contracts.{Name}?> GetAsync(string id) => _ef.{Name}s.GetAsync(id);

// Create — DataResult<T> includes mutation flag for event decisions
public Task<DataResult<Contracts.{Name}>> CreateAsync(Contracts.{Name} value) => _ef.{Name}s.CreateAsync(value);

// Update
public Task<DataResult<Contracts.{Name}>> UpdateAsync(Contracts.{Name} value) => _ef.{Name}s.UpdateAsync(value);

// Delete — DataResult (no value) carries mutation flag only
public Task<DataResult> DeleteAsync(string id) => _ef.{Name}s.DeleteAsync(id);
```

**`DataResult<T>` return types are required for Create and Update** — they carry the mutation flag that services use to decide whether to publish an event.

---

## Path C — Add a Query Method with Dynamic Filtering/Ordering

Define `QueryArgsConfig` once as a `private static readonly` class-level field:

```csharp
private static readonly QueryArgsConfig _queryConfig = QueryArgsConfig.Create()
    .WithFilter(filter => filter
        .WithDefaultModelPrefix("{Name}")
        .AddField<string>(nameof(Contracts.{Base}.{Field}), c => c
            .WithOperators(QueryFilterOperator.EqualityOperators | QueryFilterOperator.StartsWith)
            .AsUpperCase())
        .AddField<string>(nameof(Contracts.{Base}.Text), c => c
            .WithOperators(QueryFilterOperator.StringFunctions)
            .AsUpperCase())
        .AddReferenceDataField<Contracts.{RefData}>(nameof(Contracts.{Base}.{RefData}), "{RefData}Code",
            c => c.WithModelPrefix(null)))
    .WithOrderBy(orderby => orderby
        .WithDefaultModelPrefix("{Name}")
        .AddField(nameof(Contracts.{Base}.{Field}), c => c.WithDefault().WithAlwaysInclude())
        .AddField(nameof(Contracts.{Base}.Text)));
```

In the query method:

```csharp
public async Task<ItemsResult<Contracts.{Name}Lite>> QueryAsync(QueryArgs? query, PagingArgs? paging)
{
    var parsed = _queryConfig.Parse(query).ThrowOnError();

    // Compose the base query with any required joins before applying parsed filters.
    var q =
        from e in _ef.{Name}s.Model.Query()
        // optional join:
        // join r in _ef.RelatedModel.Query() on e.RelatedCode equals r.Code into rg
        // from r in rg.DefaultIfEmpty()
        select new { Entity = e };

    return await q
        .Where(parsed)
        .OrderBy(parsed)
        .ToMappedItemsResultAsync(x => new Contracts.{Name}Lite
        {
            Id = x.Entity.Id,
            // ... project fields
        }, paging);
}
```

Expose the query schema for `$query` documentation endpoints:

```csharp
public Task<JsonElement> QuerySchemaAsync() => Task.FromResult(_queryConfig.ToJsonSchema());
```

**`QueryArgsConfig` requires `global using CoreEx.Data.Querying;` in `GlobalUsing.cs`** — add it if missing.

---

## Path D — Result&lt;T&gt; Pipeline (ROP / Railway Oriented Programming)

Use when the project has elected to use `Result<T>` pipelines for explicit failure propagation instead of exceptions. This is a per-project or per-service style choice — not tied to the presence of a Domain layer, though DDD aggregate domains often use it naturally because aggregate mutation methods already return `Result<T>`. Use `*WithResultAsync` variants and compose with `.GoAsync` / `.Then` / `.ThenAs` / `.ThenAsAsync`:

> **Domain aggregate vs Contract-level ROP:** The patterns below use `Domain.{Name}` and explicit `{Name}Mapper` / `{Name}IntoMapper` — this is the DDD case where `_ef.{Name}s` is `EfDbModel<Persistence.{Name}>`. For contract-level ROP (no Domain layer), use `EfDbMappedModel.*WithResultAsync` directly — `GetWithResultAsync` already returns `Result<Contracts.{Name}>` with no extra mapping step needed.

```csharp
// Get — Persistence → Domain via mapper
public Task<Result<Domain.{Name}>> GetAsync(string id) => Result
    .GoAsync(() => _ef.{Name}s.GetWithResultAsync(id))
    .ThenAs(model => {Name}Mapper.Map(model));

// Create — Domain → Persistence → Domain via into-mapper + create
public Task<Result<Domain.{Name}>> CreateAsync(Domain.{Name} value) => Result
    .Go(() =>
    {
        var model = new Persistence.{Name}();
        {Name}IntoMapper.MapInto(value, model);
        return model;
    })
    .ThenAsAsync(model => _ef.{Name}s.CreateWithResultAsync(model))
    .ThenAs(m => {Name}Mapper.Map(m));

// Update — load model, apply into-mapper, save, map back
public Task<Result<Domain.{Name}>> UpdateAsync(Domain.{Name} value) => Result
    .GoAsync(() => _ef.{Name}s.GetWithResultAsync(value.Id))
    .Then(model =>
    {
        {Name}IntoMapper.MapInto(value, model);
        return model;
    })
    .ThenAsAsync(model => _ef.{Name}s.UpdateWithResultAsync(model))
    .ThenAs(m => {Name}Mapper.Map(m));
```

**`*WithResultAsync` returns `Result<DataResult<T>>`** — chain `.ThenAs(dr => dr.Value)` to unwrap if the downstream consumer needs `Result<T>` directly.

**Domain ↔ Persistence mappers are one-way** (`Mapper<TPersistence, TDomain, TSelf>`) — they map from persistence to domain only; use a separate `{Name}IntoMapper` (`IntoMapper<TDomain, TPersistence, TSelf>`) for the write direction.

---

## Phase 2 — Validate and Test

1. `dotnet build` — no errors or warnings.
2. Verify the repository interface in `Application/Repositories/` declares all new methods.
3. Verify the service calling the repository handles `null` returns (Get) and checks `DataResult.HasMutations` before publishing events (Create/Update/Delete).
4. **Offer to create or update the matching integration test** in `*.Test.Integration/Repositories/` or `*.Test.Api/`. Integration tests seed via `Data/*.yaml` and assert round-trip fidelity.
5. Check `GlobalUsing.cs` for required namespace imports (`CoreEx.Data.Querying`, `{Solution}.Infrastructure.Mapping`).

---

## Guardrails

- **Never write raw `DbContext` CRUD** — always use EfDb delegate methods (`GetAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`).
- **Do not reference Infrastructure from Application** — Infrastructure implements Application interfaces, never the other way.
- **Do not create or edit `*.g.cs` / `*.g.sql` files** — these are generated by `*.Database` / `*.CodeGen` tooling.
- **`BiDirectionMapper.OnMap` — two overloads, same name, different source type** — do not invent `OnMapToPrimary` / `OnMapToSecondary`.
- **`Id` must be mapped explicitly** in `OnMap` — the base does not auto-map it.
- **`ETag` and `ChangeLog` must NOT be mapped** in `OnMap` — the base mapper owns them; the model has no `RowVersion` member.
- **`DataResult<T>` for Create/Update, `DataResult` for Delete** — do not return `T` directly; the mutation flag is needed for event decisions.
- **`[ScopedService<IInterface>]` on every repository** — do not add `AddScoped<>()` calls in `Program.cs` for repositories.
- **`QueryArgsConfig` is `private static readonly`** — never instantiate per-request; parse once per call with `.Parse(query).ThrowOnError()`.
- **Match the database package to the project**: PostgreSQL → `CoreEx.Database.Postgres`, `PostgresDatabase`, `UseNpgsql`; SQL Server → `CoreEx.Database.SqlServer`, `SqlServerDatabase`, `UseSqlServer`. Check the project's `Program.cs` — do not introduce the wrong provider.
