---
applyTo: "**/*.Test*/**/*.cs"
description: "Test conventions: test project types (Api/Unit/Subscribe/Relay), base classes, one-time setup patterns, and assertion helpers"
tags: ["testing", "unit-tests", "integration-tests", "test-helpers", "nunit"]
---

# Test Conventions

## NuGet / Project References

| Package | Key types provided |
|---|---|
| `CoreEx.UnitTesting` | Base testers and common helpers: `WithApiTester<T>`, `WithGenericTester<T>`, `Test.Http()`, `Test.Http<T>()`, `Test.Scoped()`, `Test.ScopedType<T>()`, `Test.ClearFusionCacheAsync()`, `Test.ReplaceHttpClientFactory()`; database helpers: `Test.MigrateSqlServerDataAsync<T>()`, `Test.UseExpectedSqlServerOutboxPublisher()`, `.ExpectSqlServerOutboxEvents()`, `.ExpectNoSqlServerOutboxEvents()`, `Test.MigratePostgresDataAsync<T>()`, `Test.UseExpectedPostgresOutboxPublisher()`, `.ExpectPostgresOutboxEvents()`, `.ExpectNoPostgresOutboxEvents()`; messaging helpers: `Test.UseExpectedAzureServiceBusPublisher()`, `Test.GetAndClearAzureServiceBusAsync()`; ASP.NET Core assertions: `.ExpectIdentifier()`, `.ExpectETag()`, `.ExpectChangeLogCreated()`, `.ExpectJsonFromResource()`, `.AssertCreated()`, `.AssertOK()`, `.AssertBadRequest()`, `.AssertErrors()`, `.AssertJsonFromResource()`, `.AssertLocationHeader()` |
| `UnitTestEx` | `MockHttpClientFactory`, `MockHttpClientRequest`, `.WithJsonResourceBody()`, `.WithAnyBody()`, `.Respond.With()`, `.Respond.WithJsonResource()`, `.Verify()` |
| `NUnit` | `[TestFixture]`, `[Test]`, `[OneTimeSetUp]` |
| `AwesomeAssertions` | `.Should()`, `.Be()`, `.HaveCount()` |

## Project Types

| Project suffix | Base class | Scope |
|---|---|---|
| `*.Test.Api` | `WithApiTester<Program>` | Full integration — real DB, cache, outbox, HTTP |
| `*.Test.Unit` | `WithGenericTester<EntryPoint>` | Component/unit — isolated, no infrastructure |
| `*.Test.Subscribe` | `WithApiTester<Program>` | Integration over subscriber host |
| `*.Test.Relay` | `WithApiTester<Program>` | Integration over relay host |

**Rule**: intra-domain dependencies (database, cache, outbox) are real; inter-domain HTTP calls and direct broker publishes are always mocked.

---

## Test Responsibility — what goes where (do not duplicate)

The two test projects have **distinct, non-overlapping jobs**. Decide where a behaviour belongs by what it needs to exercise, and assert it in **one** place only.

| Concern | Where | Why |
|---|---|---|
| **Service orchestration & repository logic** — CRUD round-trips, identifier assignment on create, persistence + mapping, query/filter behaviour, **soft-delete filtering**, eventing (outbox subject/destination), concurrency (ETag/`If-Match`), idempotency, error→HTTP mapping (404/409/412/428) | **`*.Test.Api`** (intra-domain integration) | These are *integration* behaviours — they only have meaning over the real DB/cache/outbox and the real host pipeline. Mocking them would test the mock, not the system. |
| **Isolated component logic** — **validators**, pure mappers, calculations, value-conversion helpers, and other logic with no infrastructure dependency | **`*.Test.Unit`** | Fast, exhaustive, no DB. The natural home for enumerating every rule/branch of a validator or a pure function. |

**Do not repeat the same assertion in both projects.** Concretely:

- **Validator rules** are proven **exhaustively in unit tests** (every mandatory/range/format/cross-field rule). In the API tests, do **not** re-enumerate them — assert **one** representative `AssertBadRequest()` + `AssertErrors(...)` case to confirm the validator is *wired into the pipeline*, then move on. **But `AssertErrors` is an exact match — not a subset.** It fails unless the listed errors are **exactly** the set the input produces (none missing, none extra). So "one representative case" means **craft the input to produce exactly the errors you assert** — e.g. send a value valid in every respect *except* the one rule under test — rather than posting an empty object (which fires *all* mandatory errors) and listing only some. (See the validators guidance — "Unit Tests".)
- **Service/repository behaviour** is proven in the **API tests** over the real database. Do **not** re-create it in unit tests with a mocked repository/UoW — that would assert the mock's configured behaviour, not the real persistence/mapping/eventing.
- When a behaviour *could* sit in either, prefer the layer that exercises it **without mocking the thing under test**: validator → unit; anything touching the DB, cache, outbox, or HTTP pipeline → API.

The goal is a single source of truth per behaviour: unit tests guard the rules and pure logic; API tests guard the wired-up, persisted, evented system.

---

## One-Time Setup

Every integration test class must have a `[OneTimeSetUp]` method. Order of operations is fixed:

1. Migrate + seed the domain database.
2. Clear the hybrid cache.
3. Register event-capture publishers.
4. Set up inter-domain HTTP mocks (for domains with cross-domain adapters).

**(SQL Server example):**
```csharp
[OneTimeSetUp]
public async Task OneTimeSetUpAsync()
{
    await Test.MigrateSqlServerDataAsync<TestData>(DbMigration.ConfigureMigrationArgs).ConfigureAwait(false);
    await Test.ClearFusionCacheAsync().ConfigureAwait(false);

    Test.UseExpectedSqlServerOutboxPublisher();
    Test.UseExpectedAzureServiceBusPublisher();

    var mcf = MockHttpClientFactory.Create();
    _mockHttpReserveRequest = mcf.CreateClient("ProductsApi")
        .Request(HttpMethod.Post, "api/inventory/reserve");
    Test.ReplaceHttpClientFactory(mcf);
}
```

**(PostgreSQL example):**
```csharp
[OneTimeSetUp]
public async Task OneTimeSetUpAsync()
{
    await Test.MigratePostgresDataAsync<TestData>(DbMigration.ConfigureMigrationArgs).ConfigureAwait(false);
    await Test.ClearFusionCacheAsync().ConfigureAwait(false);

    Test.UseExpectedPostgresOutboxPublisher();
}
```

**Outbox assertion helpers are database-specific.** Use `UseExpectedPostgresOutboxPublisher` / `ExpectPostgresOutboxEvents` for PostgreSQL domains; use `UseExpectedSqlServerOutboxPublisher` / `ExpectSqlServerOutboxEvents` for SQL Server domains. Never mix them.

`DataResetFilterPredicate` in `DbMigration.ConfigureMigrationArgs` scopes the reset to the domain's own schema — multiple domains' test runs do not corrupt each other even when run concurrently.

For the **API read/mutate test classes** (see [API Tests — Structure & Generation](#api-tests--structure--generation)), pass the class's specific dataset via the **named-file overload** so read and mutate classes load only their own data: `MigrateSqlServerDataAsync<TestData>(["read-data.seed.yaml"], …)` / `MigratePostgresDataAsync<TestData>(["mutate-data.seed.yaml"], …)`. The no-argument overload loads every `Data/*.seed.yaml`, which would mix the read and mutate datasets.

---

## Test Data (seed files)

Test seed data lives under `Data/` in the `*.Test.Common` project, located via the `TestData` marker class (do not rename/move it). These are **test datasets**, distinct from the production `*.Database/Data/ref-data.seed.yaml`.

Use **one read dataset and one mutate dataset per domain** — `read-data.seed.yaml` and `mutate-data.seed.yaml` — shared across all of the domain's entities to avoid duplication. The relevant test class loads its dataset by name in `[OneTimeSetUp]`:

```csharp
await Test.MigrateSqlServerDataAsync<TestData>(["read-data.seed.yaml"], DbMigration.ConfigureMigrationArgs);   // or MigratePostgresDataAsync
```

A developer may override/extend by passing additional files in the list when a class needs bespoke data.

**Format** — `schema:` → `- <table>:` → rows, where each row is an **inline object** keyed by column name. Unlike the production `ref-data.seed.yaml` (which uses `$^<table>` + auto-id + `code: text` shorthand), test data uses a **plain `- <table>:`** entry (no `$`/`$^` — it is inserted into a freshly-reset DB) and rows that **list the columns explicitly**.

> **⚠️ All identifiers — schema, table, AND column names — must match the database's actual casing for the chosen provider** (i.e. exactly what the migration scripts created). DbEx does not case-fold them, so a wrong-cased schema/table fails the seed with *"Table '…' does not exist"*:
> - **PostgreSQL (the default provider)** → **lowercase `snake_case`**: schema `bar`, table `employee`, columns `employee_id`, `first_name`, `gender_code`.
> - **SQL Server** → **`PascalCase`**: schema `Bar`, table `Employee`, columns `EmployeeId`, `FirstName`, `GenderCode`.

```yaml
# PostgreSQL (default) — lowercase schema/table, snake_case columns
bar:
  - employee:                         # table — plain, no $^ prefix
    - { employee_id: ^1, first_name: Bob, last_name: Smith, gender_code: M, salary: 50000, date_of_birth: 1990-01-01 }
    - { employee_id: ^2, first_name: Jane, last_name: Doe, gender_code: F, salary: 60000, date_of_birth: 1985-06-15 }
```

```yaml
# SQL Server — PascalCase schema/table/columns
Bar:
  - Employee:
    - { EmployeeId: ^1, FirstName: Bob, LastName: Smith, GenderCode: M, Salary: 50000, DateOfBirth: 1990-01-01 }
```

- **`^N` is a deterministic GUID** — `^1` equals `1.ToGuid()`. Use it for the **identifier** and for any **GUID foreign-key reference** to another seeded row (e.g. a `movement` row with `{ product_id: ^1 }` points at the product seeded as `^1`). This is what lets a test target a specific row by the same `N.ToGuid()`.
- Reference data is linked **by code** — `gender_code: M` (PostgreSQL) / `GenderCode: M` (SQL Server), not an id/FK.
- Set scenario flags explicitly where a test needs them — e.g. `is_deleted: true`, `is_inactive: true` (PostgreSQL casing shown).

> ### ⚠️ Writing the GUID literal in resource files (`.res.json` / `.event.json`)
>
> In seed YAML and test code, use `^N` / `N.ToGuid()` — **never hand-write the GUID**. But resource files can't call code, so when a deterministic id/FK must appear as a **literal string** you must compute `N.ToGuid()` correctly. Two rules the agent gets wrong:
>
> 1. **The number goes in the FIRST segment, not the last.** `1.ToGuid()` is `00000001-0000-0000-0000-000000000000`, **not** `00000000-0000-0000-0000-000000000001`.
> 2. **The first segment is the number in lowercase HEXADECIMAL, zero-padded to 8 digits — not decimal.** It is *not* a straight digit substitution; convert to hex. (For `N ≤ 9` hex and decimal coincide, which hides the bug; it surfaces at `N ≥ 10`.)
>
> | `N` | first segment (hex) | full `N.ToGuid()` literal |
> |---|---|---|
> | 1 | `00000001` | `00000001-0000-0000-0000-000000000000` |
> | 2 | `00000002` | `00000002-0000-0000-0000-000000000000` |
> | 12 | `0000000c` | `0000000c-0000-0000-0000-000000000000` |
> | 16 | `00000010` | `00000010-0000-0000-0000-000000000000` |
> | 255 | `000000ff` | `000000ff-0000-0000-0000-000000000000` |
> | 1000 | `000003e8` | `000003e8-0000-0000-0000-000000000000` |
>
> Formula: `N.ToString("x8") + "-0000-0000-0000-000000000000"`. When in doubt, prefer excluding the volatile `id` from the resource (the `Expect*` helpers / `.AssertJsonFromResource(..., "id")` auto-exclude it) so no literal is needed; only write the literal for a **non-id** field that genuinely carries a deterministic GUID (e.g. a foreign-key column in a query result or event payload).

In the test, reference the seeded row by the same number:

```csharp
Test.Http().Run(HttpMethod.Get, $"/api/employees/{1.ToGuid()}").AssertOK();   // the EmployeeId: ^1 row
```

---

## API Tests — Structure & Generation

Intra-domain API (integration) tests run the real host over the real DB/cache/outbox (`WithApiTester<{Solution}.Api.Program>` — reference `Program` fully-qualified; it is `public`, no extra `using` needed). The `*.Test.Api` project's `GlobalUsing.cs` (shipped by the template) already provides the imports and the two aliases the setup uses — **`DbMigration`** (= `{Solution}.Database.Program`, for `DbMigration.ConfigureMigrationArgs`) and **`TestData`** (= `{Solution}.Test.Common.TestData`, the embedded-data marker) — plus `{Solution}.Contracts`, CoreEx, UnitTestEx, AwesomeAssertions, NUnit. Use those aliases; don't re-derive them.

Organise them **per entity**, split read vs mutate, one file per operation:

| Class (per entity) | Endpoints | `[OneTimeSetUp]` seeds |
|---|---|---|
| `XxxReadTests` | reads (GET/query) | `read-data.seed.yaml` |
| `XxxMutateTests` | mutations (POST/PUT/PATCH/DELETE) | `mutate-data.seed.yaml` |

Each is a **`partial class`**; the `[OneTimeSetUp]` lives in `XxxReadTests.cs` / `XxxMutateTests.cs`, and **each operation goes in its own partial sub-file** `Xxx{Read|Mutate}Tests.{Operation}.cs` (Operation = the controller operation: `Get`, `Query`, `Create`, `Update`, `Patch`, `Delete`, …) — isolating operations for discoverability and maintenance. Example for `Employee`:

```
EmployeeReadTests.cs            // [OneTimeSetUp] → read-data.seed.yaml + ClearFusionCacheAsync
EmployeeReadTests.Get.cs
EmployeeReadTests.Query.cs
EmployeeMutateTests.cs          // [OneTimeSetUp] → mutate-data.seed.yaml (+ outbox publisher, HTTP mocks)
EmployeeMutateTests.Create.cs
EmployeeMutateTests.Update.cs
EmployeeMutateTests.Patch.cs
EmployeeMutateTests.Delete.cs
```

```csharp
// EmployeeMutateTests.cs
public partial class EmployeeMutateTests : WithApiTester<MyApp.Api.Program>
{
    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        await Test.MigrateSqlServerDataAsync<TestData>(["mutate-data.seed.yaml"], DbMigration.ConfigureMigrationArgs).ConfigureAwait(false);
        await Test.ClearFusionCacheAsync().ConfigureAwait(false);
        Test.UseExpectedSqlServerOutboxPublisher();   // provider-specific; see One-Time Setup
    }
}

// EmployeeMutateTests.Create.cs
public partial class EmployeeMutateTests
{
    [Test]
    public void Create_Success() => /* Test.Http<Employee>()… .AssertCreated()… */;
}
```

**Isolation:** read and mutate are separate classes with separate seed files, so mutations never disturb read expectations. Within a `XxxMutateTests` class, write each test to be **independent** — act on a distinct seeded id or create its own data; never depend on another test's side effects or run order.

> **One seed row per _destructive test_ — not per operation.** Every test that **writes** to the database (Update, Patch, Delete, …) must target its **own** `^N` id. It is **not** enough to give each *operation* a distinct row — two **tests** that mutate the same row fail non-deterministically because **NUnit randomises execution order** (e.g. a Delete test running before an Update test that shares the row). Provision one row per destructive test **up front** in `mutate-data.seed.yaml`; don't add rows reactively after a collision surfaces. Canonical assignment for full CRUD:
>
> | `^N` | Test | Notes |
> |---|---|---|
> | `^1` | `Update_Success` | `Update_NotFound` uses a non-existent id; `Update_ConcurrencyError` only reads `^1` (rolls back) so it may share it |
> | `^2` | `Delete_*` (exists → idempotent flow) | the row is removed by the test |
> | `^3` | `Patch_Success` | |
>
> Non-mutating tests (Get, Query, 304, validation bad-requests) can freely share read rows — the rule is specifically about tests that **commit a write**.

**Expected `.req.json` / `.res.json` resources** (the JSON representation of the request/response) live under `Resources/{TestClass}/…` and are referenced via `.ExpectJsonFromResource(...)` / `.AssertJsonFromResource("EmployeeReadTests.Employee_Get_Found.res.json", "etag", "changelog")` (exclude volatile fields). **Pre-author them from the seed values** — you control the seed, so you can write the expected JSON up front (remember to exclude/expect the volatile `id`/`etag`/`changelog`); then **run once and reconcile** any remaining differences from the actual output (they are intentionally copy-paste-friendly). Expect the first run to need a small fix-up; that's normal, not a failure to avoid. They scale better than inline assertions as entities grow.

> **Agent instruction — co-design seed, tests, and resources together.** These three must agree, so author them in order:
> 1. **Seed first** — add/extend the domain's `read-data.seed.yaml` / `mutate-data.seed.yaml` with the known rows the tests will reference, as object rows with deterministic **`^N` ids** (= `N.ToGuid()`); for mutate, the baselines a test needs — e.g. an existing SKU for a duplicate-conflict test, a row to update/delete.
> 2. **Tests next** — one partial file per operation; reference seeded rows via `n.ToGuid()` / known codes; keep mutate tests independent.
> 3. **Resources last** — capture `.res.json`/`.req.json` from the actual run; the response resource must match the seeded values.
>
> Cover, per operation: **Get** (found, not-found, ETag/not-modified); **Query** (filter, order, paging, field selection); **Create** (success + Location + outbox event, bad-data validation, duplicate/conflict, idempotency-key); **Update** (success + ETag/concurrency, not-found); **Patch** (merge success, not-found); **Delete** (idempotent — always 204, never 404): **get → delete → get → delete** = exists → 204+event → now 404 → 204 with **no** event. Assert outbox events on mutating success paths (provider-specific `ExpectXxxOutboxEvents`) and `ExpectNoXxxOutboxEvents` on failure paths.

---

## API Test Pattern

Use the `Test.Http()` / `Test.Http<T>()` fluent chain: **set expectations → execute → assert**.

```csharp
// GET
Test.Http()
    .Run(HttpMethod.Get, $"/api/products/{1.ToGuid()}")
    .AssertOK()
    .AssertJsonFromResource("ReadTests.Product_Get_Found.res.json", "etag", "changelog");

// POST — PostgreSQL domain (Postgres outbox)
var created = Test.Http<Product>()
    .ExpectIdentifier()
    .ExpectETag()
    .ExpectChangeLogCreated()
    .ExpectJsonFromResource("ProductMutateTests.Create_Success.res.json")
    .ExpectPostgresOutboxEvents(e => e
        .AssertWithValue("contoso", "contoso.products.product.created.v1"))
    .Run(HttpMethod.Post, "/api/products", product)
    .AssertCreated()
    .AssertLocationHeader(r => new Uri($"/api/products/{r!.Id}", UriKind.Relative))
    .Value!;

// POST — SQL Server domain (SQL Server outbox)
Test.Http<Basket>()
    .ExpectSqlServerOutboxEvents(e => e
        .AssertWithValue("contoso", "contoso.shopping.basket.checkedout.v1"))
    .Run(HttpMethod.Post, $"/api/baskets/{basketId}/checkout", checkoutRequest)
    .AssertOK();

// Validation error
Test.Http()
    .Run(HttpMethod.Post, "/api/products", invalidProduct)
    .AssertBadRequest()
    .AssertErrors("Text is required.", "Price must be greater than or equal to zero.");

// Assert no events on failure path
Test.Http()
    .ExpectNoSqlServerOutboxEvents()
    .Run(HttpMethod.Post, $"/api/baskets/{basketId}/checkout")
    .AssertBadRequest();
```

### Expectation helpers (assert *and* auto-exclude from the JSON compare)

These `Test.Http<T>()` expectations both **assert the value is present** and **automatically exclude it from the JSON comparison** — so the `.res.json` should **omit** these fields and you do **not** list them as manual excludes:

- `ExpectIdentifier()` — asserts `Id` has a value; excludes it from the compare.
- `ExpectETag()` — asserts `ETag` has a value; excludes it.
- `ExpectChangeLogCreated()` / `ExpectChangeLogUpdated()` — assert the `ChangeLog` create/update values are set; exclude `changelog`.

(For a plain GET via `.AssertJsonFromResource(...)` *without* these expectations, exclude the volatile fields manually — e.g. `"etag", "changelog"` — as in the GET example above.)

### Update (PUT) and Patch with ETag (concurrency)

Send the ETag for concurrency-controlled mutations. **Fetch the current entity first** to get its `ETag`, then PUT with `If-Match`:

```csharp
// Arrange: get the current row (the EmployeeId: ^1 seed) to obtain its ETag.
var val = Test.Http<Product>()
    .Run(HttpMethod.Get, $"/api/products/{1.ToGuid()}").AssertOK().Value!;
val.Text = "Updated text";

// Act/Assert: PUT with If-Match.
Test.Http<Product>()
    .Run(HttpMethod.Put, $"/api/products/{val.Id}", val, requestModifier: r => r.WithIfMatch(val.ETag))
    .AssertOK();
```
> If the serialized request body already contains `ETag` and no `If-Match` header is set, that body `ETag` is used as the concurrency token.

For **PATCH**, also set the **merge-patch content type** (the request default is plain JSON):

```csharp
Test.Http<Product>()
    .Run(HttpMethod.Patch, $"/api/products/{p.Id}", new { text = p.Text },
         requestModifier: r => r.WithIfMatch(val.ETag).WithMergePatchJsonContentType())
    .AssertOK();
```

**Concurrency error (stale ETag) → 412.** Supply a **wrong** `If-Match` and assert `AssertPreconditionFailed()`. The `If-Match` **header takes precedence** over any `ETag` in the body, so you do **not** need to clear `val.ETag` — the header value drives the concurrency check:

```csharp
var p = Test.Http<Product>().Run(HttpMethod.Get, $"/api/products/{6.ToGuid()}").AssertOK().Value!;
p.Text += " Updated";
Test.Http()
    .ExpectNoSqlServerOutboxEvents()                          // a rejected update commits nothing → emits nothing
    .Run(HttpMethod.Put, $"/api/products/{p.Id}", p, requestModifier: r => r.WithIfMatch("AAAAAAAA"))
    .AssertPreconditionFailed();                              // 412 — NOT AssertConflict()/409
```

This is **412 Precondition Failed** (`ConcurrencyException`), not 409. Use `.AssertPreconditionFailed()`. (409/`AssertConflict()` is for duplicate-key/business conflicts only — see the HTTP assertion table.)

### Conditional GET with If-None-Match → 304 Not Modified

For a conditional read, use the **`WithIfNoneMatch(...)`** request-modifier helper (the read-side counterpart of `WithIfMatch`) and assert `AssertNotModified()`. **Fetch once to obtain the response ETag**, then re-GET with it:

```csharp
// 1. GET to obtain the current ETag from the response headers.
var r = Test.Http()
    .Run(HttpMethod.Get, $"/api/products/{1.ToGuid()}")
    .AssertOK()
    .Response;

// 2. Conditional GET with If-None-Match → 304.
Test.Http()
    .Run(HttpMethod.Get, $"/api/products/{1.ToGuid()}", requestModifier: rm => rm.WithIfNoneMatch(r.Headers.ETag!.Tag))
    .AssertNotModified();
```

> **Use the `WithIfNoneMatch(...)` helper — do not set the header by hand.** `If-None-Match` requires a **quoted entity-tag** (`"<value>"`, RFC 7232); `r.Headers.Add("If-None-Match", etag)` throws **`FormatException`** on an unquoted value. `WithIfNoneMatch(...)` (and passing `response.Headers.ETag.Tag`, which is already quoted) handles the format for you — mirror it rather than reaching for raw header manipulation.

### Update of a non-existent id → 404 (ETag + value still required)

Concurrency is checked **before** existence. So a "PUT a non-existent id → `NotFound`" test must **still send a valid ETag and a full value body** — otherwise the precondition check fires first and you get **`428 Precondition Required`** ("*A concurrency error occurred; an ETag is required either as an If-Match header (preferred) or specified within the request body (where supported).*"), not the `404` the test intends.

```csharp
// ✅ Correct — supply an ETag (any well-formed value) and a complete body; the 404 comes from the row not existing.
var val = new Product { Id = 404.ToGuid().ToString(), Text = "Does not exist" /* ...all mandatory fields... */ };
Test.Http<Product>()
    .Run(HttpMethod.Put, $"/api/products/{val.Id}", val, requestModifier: r => r.WithIfMatch("any-etag"))
    .AssertNotFound();

// ❌ Wrong — no If-Match / no body ETag → 428 Precondition Required, never reaches the not-found check.
Test.Http<Product>()
    .Run(HttpMethod.Put, $"/api/products/{404.ToGuid()}", val)
    .AssertNotFound();   // fails: actual is 428
```

The ETag value need not match anything (the row doesn't exist) — it only has to be **present** to clear the precondition gate. The body must still satisfy model validation (all mandatory fields), since validation also precedes the repository lookup.

### Get of a soft-deleted row → 404 (when `IsDeleted` is supported)

When the entity supports soft-delete (`IsDeleted` column), a row flagged deleted must be **invisible to reads** — a `GET` of it returns **`404 Not Found`**, exactly as if it never existed. This needs its own test because a row that is *present in the table* but filtered out is a different code path from a row that was never seeded.

1. **Seed a deleted row** — add a row to the domain's read seed file with the soft-delete flag set (provider casing — `is_deleted` PostgreSQL / `IsDeleted` SQL Server):

```yaml
# PostgreSQL (default)
bar:
  - product:
    - { product_id: ^9, name: Ghost, sku: GHOST-1, is_deleted: true }   # soft-deleted — must not be readable
```

2. **Assert the GET 404s** (and that it does **not** appear in a list/query):

```csharp
// Direct get of the soft-deleted row → 404.
Test.Http().Run(HttpMethod.Get, $"/api/products/{9.ToGuid()}").AssertNotFound();
```

The point of the test is that the soft-delete filter is actually applied on read — a missing filter would return the row with `200 OK` instead of `404`. (Optionally also assert the row is absent from a collection/query result.)

### Delete — get → delete → get → delete (idempotent)

The canonical delete test is a **four-step** flow that proves both the delete and its **idempotency**: GET (exists) → DELETE (204 + event) → GET (now 404) → DELETE again (204, **no** event). `DELETE` **always** returns **204 No Content** — **never 404**, even for a non-existent or already-deleted id; only the **first** delete (where the row existed) emits the event. The 404 belongs to the **GET**, not the DELETE.

```csharp
// 1. Confirm it exists (the EmployeeId: ^2 seed).
Test.Http().Run(HttpMethod.Get, $"/api/products/{2.ToGuid()}").AssertOK();

// 2. Delete — 204, and a "deleted" outbox event. Delete returns NO value body → assert METADATA + KEY (not value);
//    the key is the deleted id carried via .WithKey(id); the subject has NO version suffix.
Test.Http()
    .ExpectPostgresOutboxEvents(e => e.AssertMetadata("contoso", "contoso.products.product.deleted", 2.ToGuid().ToString()))
    .Run(HttpMethod.Delete, $"/api/products/{2.ToGuid()}")
    .AssertNoContent();

// 3. Confirm it is gone — the GET now 404s (it is the GET, not the DELETE, that returns not-found).
Test.Http().Run(HttpMethod.Get, $"/api/products/{2.ToGuid()}").AssertNotFound();

// 4. Repeat delete is idempotent — still 204, but emits NO event.
Test.Http()
    .ExpectNoPostgresOutboxEvents()
    .Run(HttpMethod.Delete, $"/api/products/{2.ToGuid()}")
    .AssertNoContent();
```

A delete of a **non-existent** id behaves like step 4 — 204 No Content with no event. **Never assert `AssertNotFound()` on a `DELETE`.**

### Outbox event assertions — destination & subject

Assert published events with `ExpectXxxOutboxEvents(e => …)` (provider-specific). **Pick the assertor by whether the event carries a value:**

- **`.AssertWithValue(destination, subject)`** — for **value-carrying** events (Create/Update). It reconstructs the expected `EventData` **from the API's returned value** and JSON-compares the event body. Only valid when the operation returns a value.
- **`.AssertMetadata(destination, subject, key)`** — for **no-value** events (**Delete**, or any operation returning `204 No Content`). It asserts the **metadata only** — destination + subject — plus the **`key`** (the `CloudEvent.Subject`, i.e. the `EventData.Key` set via `.WithKey(id)` in the service). There is no value to compare, so `AssertWithValue` would have nothing to reconstruct from — use `AssertMetadata` and pass the deleted id (e.g. `2.ToGuid().ToString()`) as the key.

The `destination` and `subject` strings:

- **`destination`** — the topic/queue the event is published to: the `CoreEx:Events:Destination` value from `appsettings.json` (the domain's messaging topic, e.g. `contoso`). Do not assume it equals the subject's first segment.
- **`subject`** — composed as **`{solutionname}.{domainname}.{entity}.{action}`** + an **optional `.v{major}` version suffix**, all **lower-case**, dot-separated:
  - `solutionname` / `domainname` — from `CoreEx:Host:SolutionName` / `:DomainName` in `appsettings.json` (lower-cased; `SolutionName` may itself contain dots, e.g. `my.foo`).
  - `entity` — the entity/contract name (e.g. `employee`).
  - `action` — the **past-tense `EventAction`** set in the **service code** (`EventData.CreateEventWith(v, EventAction.Created)` → `created`; `Updated` → `updated`; `Deleted` → `deleted`).
  - **`.v{major}` version suffix — present *only when the event carries a value*** (e.g. Create/Update). Its value is the **major** of the contract's `[Schema("v2.0")]` attribute → `.v2`; **unannotated defaults to `.v1`**. An event with **no value** (e.g. Delete) has **no version suffix at all**.

```csharp
// Create — has a value → AssertWithValue, version suffix (default v1, or the [Schema] major).
.ExpectPostgresOutboxEvents(e => e.AssertWithValue("contoso", "contoso.products.product.created.v1"))
// Delete — no value → AssertMetadata with the key (the deleted id); no version suffix.
.ExpectPostgresOutboxEvents(e => e.AssertMetadata("contoso", "contoso.products.product.deleted", 2.ToGuid().ToString()))
```

So a `[Schema("v2.0")] Product` create event is `…product.created.v2`. If unsure of the exact subject, **run the test once** — the assertion failure reports the actual event subject; copy it verbatim. **Delete** events carry no value body but do carry the key (`EventData.CreateEvent<T>(EventAction.Deleted).WithKey(id)`); assert them with **`AssertMetadata(destination, "…deleted", key)`** — the unversioned `…deleted` subject plus the id as the key (there is no value to JSON-compare, so `AssertWithValue` does not apply).

### HTTP assertion methods

Use the `Assert*` helper matching the expected status; for anything not listed, assert `.Response.StatusCode` directly.

| Outcome | Helper |
|---|---|
| 200 OK | `.AssertOK()` |
| 201 Created | `.AssertCreated()` (pair with `.AssertLocationHeader(...)`) |
| 204 No Content (DELETE) | `.AssertNoContent()` |
| 304 Not Modified (ETag / If-None-Match) | `.AssertNotModified()` |
| 400 Bad Request | `.AssertBadRequest()` + `.AssertErrors("…")` |
| 404 Not Found | `.AssertNotFound()` |
| 409 Conflict — **duplicate key / business conflict** (`DuplicateException` / `ConflictException`) | `.AssertConflict()` |
| 412 Precondition Failed — **stale/mismatched ETag** (`ConcurrencyException`) | `.AssertPreconditionFailed()` |
| 428 Precondition Required — **no ETag supplied** on a concurrency-controlled mutation | `.Assert(HttpStatusCode.PreconditionRequired)` |

> **409 vs 412 — different problems, don't conflate.** A **concurrency** failure (the supplied ETag doesn't match the current row) is **412 Precondition Failed** (`ConcurrencyException`) — use `.AssertPreconditionFailed()`. **409 Conflict** is only a **duplicate-key / unique-constraint or business conflict** (`DuplicateException`/`ConflictException`) — e.g. creating a second row with an existing SKU. A stale-ETag update is **never** 409. (And a mutation that supplies **no** ETag at all fails the precondition gate with **428**, not 412 — see "Update of a non-existent id".)

### `Test.Http()` vs `Test.Http<T>()`

Use **`Test.Http<T>()`** when you need the deserialized response `.Value` (e.g. to capture the created entity, then re-`Get` and compare). Use the untyped **`Test.Http()`** when you only assert status / `.AssertJsonFromResource(...)` / errors.

### Test host configuration — leave appsettings alone

Do **not** create or edit `appsettings*.json` (including `appsettings.unittest.json`) for API tests. The test host runs via `WebApplicationFactory`, which loads the host's **Development** settings automatically — connection strings, cache, messaging, etc. are already wired. There is nothing for the agent to configure here.

## Resource-Based JSON Assertions

Expected response bodies live in `Resources/` as `.res.json` files. Reference them by dot-separated path. Exclude volatile fields as extra parameters:

```csharp
.AssertJsonFromResource("ReadTests.Product_Get_Found.res.json", "etag", "changelog");
.AssertJsonFromResource("Basket_Checkout_Insufficient_Quantity.products.res.json", "traceid");
```

Mock request bodies use `.req.json`; mock response bodies from a downstream API use `.{domain}.res.json` (prefixed with the remote domain name by convention).

**Reference-data properties serialize by their non-`Code` name, value only.** A contract `[ReferenceData<Gender>] string? GenderCode` property serializes to JSON as **`"gender": "M"`** (the Roslyn generator sets the `JsonPropertyName` to the non-`Code` suffix and emits just the code value). So in `.res.json` / `.req.json` resources — and in any inline request body — use the **non-`Code`** JSON name (`gender`, `subCategory`, `unitOfMeasure`), even though the C# contract property is `GenderCode` / `SubCategoryCode` / `UnitOfMeasureCode`. (Note the three distinct representations: C# property `GenderCode` → JSON `gender` → DB/seed column `GenderCode`/`gender_code`.)

A ref-data value (e.g. `"gender": "M"`) is **real, deterministic data — include it in the `.res.json` and do not exclude it.** The **only** volatile fields are `id`, `etag`, and `changelog` (server-assigned/timestamped); exclude those — via the `Expect*` helpers on mutations (which auto-exclude), or as explicit `.AssertJsonFromResource(..., "etag", "changelog")` arguments on a plain GET.

---

## HTTP Client Mocking

Declare `MockHttpClientRequest` fields at class level; configure responses per test; always call `.Verify()` after the action:

```csharp
// Class level
private MockHttpClientRequest _mockHttpReserveRequest = null!;

// OneTimeSetUp
var mcf = MockHttpClientFactory.Create();
_mockHttpReserveRequest = mcf.CreateClient("ProductsApi")
    .Request(HttpMethod.Post, "api/inventory/reserve");
Test.ReplaceHttpClientFactory(mcf);

// In test — success path
_mockHttpReserveRequest
    .WithJsonResourceBody("Basket_Checkout_Success.products.req.json")
    .Respond.With(HttpStatusCode.OK);
_mockHttpReserveRequest.Verify();

// In test — error path
_mockHttpReserveRequest.WithAnyBody()
    .Respond.WithJsonResource(
        "Basket_Checkout_Insufficient_Quantity.products.res.json",
        HttpStatusCode.BadRequest,
        System.Net.Mime.MediaTypeNames.Application.ProblemJson);
_mockHttpReserveRequest.Verify();
```

---

## Unit Tests — Validators

Unit tests are for logic with **no external dependencies**; any injected services are **mocked**. **Validators are the primary unit-test target** — they encode the most conditional logic. Application service orchestration is exercised by the host integration tests (`*.Test.Api` / `*.Test.Subscribe`), **not** here.

Maintain **one test class per validator**, under `*.Test.Unit/Validators/`, named `{Validator}Tests` and extending `WithGenericTester<EntryPoint>` — `EntryPoint` (in the template) configures the DI/host services the validator needs. Each `[Test]` runs inside `Test.Scoped(test => { ... })`. Invoke the validator exactly as the application does:

> **⚠️ `Test.Scoped` takes no type parameter for validator tests.** Use the **non-generic** `Test.Scoped(test => { … })` and invoke the validator via its `Default` singleton (or `new XxxValidator(deps)`). Do **not** write `Test.Scoped<XxxValidator>(v => …)` — the generic overload **resolves `XxxValidator` from DI**, but validators are **not registered in DI** (see `coreex-validators.instructions.md`), so it fails. The validator is created explicitly (`.Default` / `new`), never resolved.

> **Do not manage `ExecutionContext` in tests.** `Test.Scoped(...)` (within `WithGenericTester<EntryPoint>` / `WithApiTester<Program>`) establishes a valid `ExecutionContext` for the scope automatically — so do **not** construct, inject, mock, or otherwise set it up in a test. The ambient `Runtime` (clock/GUID) and any `ExecutionContext`-dependent rule "just work" inside the scope. Write the test as if at runtime; the harness handles the context. (The `Test.ScopedType<ExecutionContext>` you may see in **Outbox Relay** tests is a *specialised* technique for writing events directly to the outbox under a scoped context — it is **not** something validator or API tests need.)

```csharp
public class ProductValidatorTests : WithGenericTester<EntryPoint>
{
    [Test]
    public void Empty_Required() => Test.Scoped(test =>
    {
        var p = new Product();
        ProductValidator.Default.AssertErrors(p,
            ("sku", "Sku is required."),
            ("text", "Text is required."),
            ("subCategory", "Sub-category is required."),
            ("unitOfMeasure", "Unit-of-measure is required."));
    });

    [Test]
    public void Success() => Test.Scoped(test =>
    {
        var p = new Product { Sku = "X", Text = "Test", SubCategoryCode = "XC", UnitOfMeasureCode = "EA", Price = 9.99m };
        ProductValidator.Default.AssertSuccess(p);
    });
}
```

- **`Validator<T, TSelf>`** (has a `Default`) → call `XxxValidator.Default`.
- **`Validator<T>`** (injected deps) → mock the dependency with `Mock<IXxx>` configured in `[OneTimeSetUp]`, then `new XxxValidator(_mock.Object)`:

```csharp
private readonly Mock<IProductRepository> _mock = new();

[OneTimeSetUp]
public void OneTimeSetUp() => _mock.Setup(x => x.GetForReservationAsync(It.IsAny<string[]>()))
    .ReturnsAsync(new Dictionary<string, ProductReserve> { ["P1"] = new() { UnitOfMeasureCode = "EA" } });

[Test]
public void Invalid_Product() => Test.Scoped(test =>
    new MovementRequestValidator(_mock.Object).AssertErrors(req,
        ("products.P2", "Product is non-stocked and therefore cannot be transacted.")));
```

### Asserting outcomes

- **`AssertSuccess(value)`** — asserts the value passes (no errors).
- **`AssertErrors(value, (jsonName, text)…)`** — asserts the **exact** set of expected errors. Each tuple is `("<json property name>", "<expected message>")`. **Order does not matter**, but **every** produced error must be accounted for (and there must be no extras). Use **JSON** property names (camelCase) with these path forms:
  - **Nested object** — dotted: `person.address.street`.
  - **Array / list item** — `[index]`: `person.addresses[0].street`.
  - **Dictionary** — `<dictionary>.<key>.<valueProperty>`: e.g. `products.P1.unitOfMeasure` means the `products` dictionary, key `P1`, and `unitOfMeasure` is a property of that entry's value. An error on the entry's value itself is just `<dictionary>.<key>` (e.g. `products.P1`). The **actual key** is the path segment — there is no `.value` segment. The literal `key` segment (e.g. `products.key`) appears **only** when the dictionary key is itself null/empty (so there is no key value to name) — it flags the missing/blank key to the consumer.

  If unsure of the exact path a rule produces, confirm it against the validator's actual output rather than guessing.

### Expected message text

Error text derives from the standard templates in [`ValidatorStrings.cs`](https://github.com/Avanade/CoreEx/blob/main/src/CoreEx.Validation/ValidatorStrings.cs) (unless a rule overrides the whole message via `.Error(...)`). Placeholders: `{0}` = the property's localized text (label), `{1}` = the value being validated, `{2}` onward = rule-specific extras (compare-to value, max length, etc.). Compose the expected string from the template + label + extras — e.g. `MandatoryFormat` "{0} is required." → `"Sku is required."`; `CompareGreaterThanEqualFormat` "{0} must be greater than or equal to {2}." with compare-text `"zero"` → `"Price must be greater than or equal to zero."`.

> **⚠️ Label casing — sentence case, not Title Case.** The `{0}` label is derived from the property name by splitting on CamelCase and **capitalising only the first word** (the rest are lower-cased). Do **not** read the label as a title-cased echo of the property name:
>
> | Property | Label `{0}` | `… is required.` |
> |---|---|---|
> | `FirstName` | `First name` | `"First name is required."` (not "First Name") |
> | `LastName` | `Last name` | `"Last name is required."` |
> | `DateOfBirth` | `Date of birth` | `"Date of birth is required."` |
> | `Salary` | `Salary` | `"Salary is required."` |
> | `Gender` | `Gender` | `"Gender is required."` |
>
> Single-word properties are simply capitalised. Always derive the label from the **split + sentence-case** rule, not from a Title-Case reading of the identifier. (A `[Display(Name = …)]`/`.Text(…)` override replaces the derived label entirely.)

> **Exact `PrecisionScale` scale message.** A scale (decimal-places) violation uses `DecimalPlacesFormat` → `"{0} exceeds the maximum decimal places ({2})."` — e.g. `Property(p => p.Salary).PrecisionScale(18, 2)` with too many decimals → `"Salary exceeds the maximum decimal places (2)."` Note the **short** form ("maximum decimal places"), **not** "maximum specified number of decimal places". If unsure of any exact string, run the test once and copy the produced message verbatim rather than guessing.

> **Exact length-rule messages.** The string length rules all end with **"character(s) in length."** (the literal `(s)` is always present, regardless of the count) — do not shorten to "characters.":
> - `MaximumLength(n)` → `MaxLengthFormat` `"{0} must not exceed {2} character(s) in length."` — e.g. `Property(p => p.Text).MaximumLength(250)` → `"Text must not exceed 250 character(s) in length."`
> - `MinimumLength(n)` → `MinLengthFormat` `"{0} must be at least {2} character(s) in length."`
> - `Length(exact)` → `ExactLengthFormat` `"{0} must be exactly {2} character(s) in length."`

> **Quick reference — rule → exact message** (`{Label}` = sentence-cased property label per the rule above; every string verified against the master `ValidatorStrings.cs` in `CoreEx.Validation`). This covers the common rules; for anything not listed, consult `ValidatorStrings.cs` — it is the authoritative source (don't guess the wording).
>
> | Rule (fluent) | `ValidatorStrings` key | Produced message |
> |---|---|---|
> | `Mandatory()` | `MandatoryFormat` | `{Label} is required.` |
> | `None()` | `NoneFormat` | `{Label} must not be specified.` |
> | `Equal(v)` | `CompareEqualFormat` | `{Label} must be equal to {v}.` |
> | `NotEqual(v)` | `CompareNotEqualFormat` | `{Label} must not be equal to {v}.` |
> | `GreaterThan(v)` | `CompareGreaterThanFormat` | `{Label} must be greater than {v}.` |
> | `GreaterThanOrEqualTo(v)` | `CompareGreaterThanEqualFormat` | `{Label} must be greater than or equal to {v}.` |
> | `LessThan(v)` | `CompareLessThanFormat` | `{Label} must be less than {v}.` |
> | `LessThanOrEqualTo(v)` | `CompareLessThanEqualFormat` | `{Label} must be less than or equal to {v}.` |
> | `Between(min,max)` / `InclusiveBetween` | `BetweenInclusiveFormat` | `{Label} must be between {min} and {max}.` |
> | `ExclusiveBetween(min,max)` | `BetweenExclusiveFormat` | `{Label} must be between {min} and {max} (exclusive).` |
> | `MaximumLength(n)` | `MaxLengthFormat` | `{Label} must not exceed {n} character(s) in length.` |
> | `MinimumLength(n)` | `MinLengthFormat` | `{Label} must be at least {n} character(s) in length.` |
> | `Length(n)` (exact) | `ExactLengthFormat` | `{Label} must be exactly {n} character(s) in length.` |
> | `PrecisionScale(p,s)` — scale | `DecimalPlacesFormat` | `{Label} exceeds the maximum decimal places ({s}).` |
> | `PrecisionScale(p,s)` — precision | `MaxDigitsFormat` | `{Label} exceeds the maximum digits (n).` |
> | `Numeric(allowNegatives: false)` | `AllowNegativesFormat` | `{Label} must not be negative.` |
> | `Email()` | `EmailFormat` | `{Label} is an invalid e-mail address.` |
> | `Matches(regex)` | `RegexFormat` | `{Label} is invalid.` |
> | `Wildcard()` | `WildcardFormat` | `{Label} contains invalid or non-supported wildcard selection.` |
> | `.IsValid()` (ref-data) | `InvalidFormat` | `{Label} is invalid.` |
> | `Collection(minCount: n)` | `MinCountFormat` | `{Label} must have at least {n} item(s).` |
> | `Collection(maxCount: n)` | `MaxCountFormat` | `{Label} must not exceed {n} item(s).` |
> | `Duplicate()` | `DuplicateFormat` | `{Label} already exists and would result in a duplicate.` |
> | `NotFound()` | `NotFoundFormat` | `{Label} was not found.` |
> | `Immutable()` | `ImmutableFormat` | `{Label} is not allowed to change; please reset value.` |
> | `Collection(item: …)` with invalid child items | `InvalidItemsFormat` | `{Label} contains one or more invalid items.` |
>
> For the comparison rules, `{v}`/`{min}`/`{max}` are the literal values — **unless** a message-text delegate is supplied (e.g. `GreaterThanOrEqualTo(0, _ => "zero")` → `"… greater than or equal to zero."`); the delegate text replaces the value token. `AssertErrors` expects **`(jsonName, message)` tuples** (camelCase JSON property path, full message) — never bare message strings. Default texts can be overridden globally via `ValidatorStrings` / localization, so if a project customises them, the project's value wins — but the defaults above are what a stock CoreEx solution produces.

### Reference data in unit tests

Validators that use reference data (`.IsValid()`, etc.) resolve it through `EntryPoint.ReferenceDataServiceDecorator`, which loads the **real seeded data** so tests use representative values rather than invented ones. When a validator under test needs a ref-data type the decorator does not yet handle, **add a new arm to** its `GetAsync` switch — inserting it **before** the final `_ => throw …` catch-all:

```csharp
public override Task<IReferenceDataCollection> GetAsync(Type type, CancellationToken cancellationToken = default) => type switch
{
    _ when type == typeof(Gender) => Task.FromResult((IReferenceDataCollection)jdr.Deserialize<GenderCollection>("Bar.$^Gender")!),
    // ...other ref-data arms...
    _ => throw new InvalidOperationException($"Type {type.FullName} is not a known {nameof(IReferenceData)}.")   // ← never remove this catch-all
};
```

**Never remove or replace the final `_ => throw …` catch-all arm** — only add arms above it. It is the guard that surfaces an unhandled ref-data type; dropping it (e.g. "replacing the throw-only body") would silently break the decorator.

**Mirror `ReferenceDataService.g.cs` exactly — dispatch on the item type, not the collection.** The decorator's `switch` must match the generated `ReferenceDataService.g.cs` `GetAsync(Type type)`: it keys on the **reference-data item type** — `typeof(Gender)` — **not** the collection type `typeof(GenderCollection)`. (The collection appears only as the `Deserialize<GenderCollection>(...)` target.) Copy the case keys from the generated file rather than guessing; using `typeof(GenderCollection)` as the key means the arm never matches and the catch-all throws.

`Gender` is the reference-data **contract type**; `"Bar.$^Gender"` is the `{schema}.$^{Table}` key into the pre-configured seed data. **The key must mirror the seed YAML's schema and `$^Table` entry exactly, including casing** — it is case-sensitive. So it follows the **provider's casing**: PascalCase for SQL Server (e.g. `"Bar.$^Gender"`, `"Orders.$^OrderStatus"`), lower/snake_case for PostgreSQL (e.g. `"products.$^category"`). Copy the casing from the actual seed `$^<Table>` rather than assuming lower-case.

**Error path for a ref-data property is the camelCase navigation name.** A ref-data rule is written against the **typed navigation** property — `Property(x => x.Gender).IsValid()` — so its error key is the **camelCase of that navigation name**: `"gender"` (for `SubCategory` → `"subCategory"`). This is the **same** token as the serialized JSON code value (`GenderCode` serialises as `gender`), so the error path and the JSON field name coincide. Assert it as `("gender", "Gender is invalid.")` — not `("genderCode", …)` and not `("Gender", …)`. (The message label still follows the sentence-case / `[Localization]` rules above.)

**Only test valid vs not-valid — not active/inactive.** A validator's reference-data rule is the `.IsValid()` extension; assert just the two outcomes: a **valid** code (use a real seeded code) and a **not-valid** code (use a code that is not in the seed — it fails naturally, no arranging required). Do **not** write tests targeting `IReferenceData.IsActive`/`IsInactive` — active/inactive handling is built into `IsValid()`, is framework behaviour we trust, and arranging inactive data just to prove it adds cost for no real coverage.

**Adding test-only entries with `ExtendForTesting`.** The seed YAML is the **production** data set, so it may not contain entries that exercise a validator rule depending on a reference-data entity's **extended property** (a custom property added to the ref-data type). Where such a value is needed, chain `ExtendForTesting(IEnumerable<IReferenceData>)` (from `UnitTestEx`) onto the deserialized collection to append test-only items. It mutates and returns the collection, so it composes inline in the decorator's `GetAsync`:

```csharp
_ when type == typeof(Category) => Task.FromResult((IReferenceDataCollection)jdr
    .Deserialize<CategoryCollection>("products.$^category")!
    .ExtendForTesting([new Category { Id = Runtime.NewId(), Code = "X", OtherProperty = false }])),
```

Give each added entry a **unique `Id`** so it cannot collide with a seeded row, using a value appropriate to the reference data's **identifier type**:
- `string` id (the default) → `Runtime.NewId()` (a unique GUID-as-string).
- `Guid` id → `Runtime.NewGuid()`.
- `int` (or other) id → a unique value of that type that won't clash with seeded ids.

This keeps the production seed clean while letting a test arrange a code carrying the exact extended-property values the scenario needs.

**Arrange via the `{Name}Code` property, not the typed navigation property.** When constructing the entity/contract under test, set the reference-data relationship using the string **`{Name}Code`** property (e.g. `new Employee { GenderCode = "M" }`), **not** the typed `{Name}` navigation property (e.g. `Gender`). The typed property resolves its value through the `ReferenceDataOrchestrator`, which is not wired up while arranging the input — so assigning it directly is unreliable. The validator's `.IsValid()` reads from the code regardless.

```csharp
// ✅ Correct — set the code variant when arranging
var e = new Employee { FirstName = "Jo", LastName = "Bloggs", GenderCode = "M" };

// ❌ Wrong — typed nav property depends on the orchestrator (not set during arrange)
var e = new Employee { FirstName = "Jo", LastName = "Bloggs", Gender = ... };
```

### Coverage

Add as many `[Test]` methods as needed for meaningful coverage — confirm both **error** and **success** outcomes. Focus on the validator's own decisions: `Mandatory`/presence where it matters, **inter-field relationships** (`DependsOn`, conditional `When*` rules, cross-property compares), custom `OnValidateAsync` logic, and reference-data **valid vs not-valid** — construct inputs that hit each branch. Prefer clear, scenario-named methods over `[TestCase]`. Aim for coverage that is genuinely representative rather than mirroring any prior hand-crafted set.

**Skip framework-guaranteed constraints.** Do **not** write boundary tests for **length** rules (`MaximumLength`, `MinimumLength`, `Length`, `String`) — assume the declared length logic works (as with reference-data active/inactive). Such tests only re-prove built-in CoreEx behaviour and add fiddly string-padding for no real coverage; spend the effort on the conditional/business logic instead.

> For relay-style tests that need a named scoped type, use `Test.ScopedType<ExecutionContext>` (see Outbox Relay Host Tests).

---

## Subscribe Host Tests

Subscribe test classes extend `WithApiTester<Program>` over the subscriber host. The `[OneTimeSetUp]` migrates/seeds the domain DB and clears FusionCache, just like an API test. Subscribe hosts **do** have FusionCache — they are full application-layer consumers that need caching for reference data and idempotency.

```csharp
public class ProductModifySubscriberTests : WithApiTester<YourDomain.Subscribe.Program>
{
    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        await Test.MigrateSqlServerDataAsync<TestData>(DbMigration.ConfigureMigrationArgs).ConfigureAwait(false);
        await Test.ClearFusionCacheAsync().ConfigureAwait(false);

        Test.UseExpectedSqlServerOutboxPublisher();
    }
}
```

---

## Outbox Relay Host Tests

Relay tests extend `WithApiTester<Program>` over the relay host. Use `Test.ScopedType<ExecutionContext>` to write events directly to the outbox, wait for the relay background service to forward them, then assert via `Test.GetAndClearAzureServiceBusAsync()`.

```csharp
public class RelayTests : WithApiTester<YourDomain.Relay.Program>
{
    [Test]
    public async Task Outbox_Relay()
    {
        Test.ScopedType<ExecutionContext>(test =>
        {
            test.Run(async _ =>
            {
                var pub = ActivatorUtilities.GetServiceOrCreateInstance<PostgresOutboxPublisher>(test.Services);
                pub.Add("contoso", [ce1, ce2]);
                await pub.PublishAsync();

                for (int i = 0; i < 5; i++)
                    await Task.Delay(TimeSpan.FromSeconds(1));

                var list = await Test.GetAndClearAzureServiceBusAsync(
                    ServiceBusSessionReceiverOptions.CreateForTopicSubscription("contoso", "products"));

                list.Should().HaveCount(2);
            }).AssertSuccess();
        });
    }
}
```

The relay host exposes hosted-service management endpoints that can also be exercised in tests:

```csharp
Test.Http()
    .Run(HttpMethod.Post, "/hosted-services/postgres-outbox-relay-03/pause")
    .Response.StatusCode.Should().Be(HttpStatusCode.Accepted);
```

> **Troubleshooting — Service Bus emulator entity not found.** If a Relay (or any Service Bus) test fails with an error like:
> ```
> The messaging entity 'sb://sbemulatorns.servicebus.onebox.windows-int.net/<topic>/subscriptions/<subscription>' could not be found.
> ```
> (the topic/subscription path varies), the test host is reaching the emulator but the requested topic/subscription does not exist in it. **Emit to the chat output:** *"Check that the Service Bus emulator (container) is executing with the correct `/servicebus/Config.json` file."* — the emulator provisions its topics/subscriptions from that config at startup, so a missing or mismatched `Config.json` (or a container started without it) is the usual cause. This is an **environment** problem, not a test-code defect — do not "fix" it by editing the test, the subjects, or the emulator entity names.

---

## NUnit Attributes

Use `[Test]` on individual test methods. `[TestFixture]` is inherited when using `WithApiTester` or `WithGenericTester`. Do not use `[TestCase]` for integration tests — use separate named methods for clarity.

## Naming Tests

Name test methods as `{Entity}_{Action}_{Outcome}`:

```
Product_Get_Found
Product_Get_NotFound
Product_Create_Success
Product_Create_Bad_Data
Basket_Checkout_Success
Basket_Checkout_Insufficient_Quantity
```

## Do Not

- Do not use `[TestCase]` for integration tests — create separate named test methods for each scenario.
- Do not use `UseExpectedSqlServerOutboxPublisher` / `ExpectSqlServerOutboxEvents` in PostgreSQL domain tests — use the Postgres equivalents.
- Do not use `UseExpectedPostgresOutboxPublisher` / `ExpectPostgresOutboxEvents` in SQL Server domain tests — use the SQL Server equivalents.
- Do not call `ClearFusionCacheAsync()` in Outbox Relay host tests — relay hosts have no cache.
- Do not test inter-domain HTTP calls against a real API — always mock with `MockHttpClientFactory`.
- Do not call `Test.ReplaceHttpClientFactory()` inside individual tests — configure it once in `[OneTimeSetUp]`.
- Do not use `FluentAssertions` — use `AwesomeAssertions` (the `AwesomeAssertions` NuGet package).
- Do not omit `.Verify()` after a `MockHttpClientRequest` action — it confirms the mock was actually invoked.
- Do not set a typed reference-data navigation property (e.g. `Gender`) when arranging a test input — set the `{Name}Code` string (e.g. `GenderCode = "M"`); the typed property depends on the `ReferenceDataOrchestrator`, which is not set during arrange.
- Do not write validator tests for reference-data `IsActive`/`IsInactive` — assert only valid vs not-valid via `.IsValid()` (a not-valid case just uses an unseeded code); active/inactive is trusted framework behaviour. Reserve `ExtendForTesting` for rules that depend on a ref-data **extended property**.
- Do not remove or replace the final `_ => throw …` catch-all arm of `ReferenceDataServiceDecorator.GetAsync` when adding a ref-data type — insert the new arm **above** it; the catch-all must remain.
- Do not write validator tests for **length** rules (`MaximumLength`/`MinimumLength`/`Length`/`String`) — assume the declared length logic works (framework-guaranteed, like reference-data active/inactive); test conditional/business logic instead.
- Do not put an entity's read and mutate API tests in one class — split into `XxxReadTests` (seeds `read-data.seed.yaml`) and `XxxMutateTests` (seeds `mutate-data.seed.yaml`), one partial sub-file per operation (`Xxx{Read|Mutate}Tests.{Operation}.cs`).
- Do not load the whole dataset in an API read/mutate class — use the named-file `MigrateXxxDataAsync<TestData>(["read-data.seed.yaml"|"mutate-data.seed.yaml"], …)` overload so the class loads only its dataset.
- Do not use the wrong identifier casing in seed YAML — schema, table, **and** column names must match the database's actual casing for the provider (PostgreSQL = lowercase `snake_case`, the default; SQL Server = `PascalCase`). A wrong-cased schema/table fails with *"Table '…' does not exist"*. Mirror exactly what the migration created.
- Do not write order-dependent or interfering mutate tests — each must act on a distinct seeded id or create its own data.
- Do not include `id`/`etag`/`changelog` in a `.res.json` used with `ExpectIdentifier()`/`ExpectETag()`/`ExpectChangeLogCreated()`/`ExpectChangeLogUpdated()` (or list them as manual excludes) — those helpers assert presence and auto-exclude from the compare.
- Do not use the `Code`-suffixed name in test JSON (`.res.json`/`.req.json`/inline bodies) for a reference-data property — use the non-`Code` JSON name (`gender`, not `genderCode`).
- Do not omit `.WithMergePatchJsonContentType()` on a PATCH test — the request default is plain JSON, not merge-patch.
- Do not assert `AssertNotFound()` on a `DELETE` — delete is idempotent and always returns 204 No Content (the 404 belongs to the *GET* in a get→delete→get flow). Only the first delete emits an event; assert `ExpectNoXxxOutboxEvents()` on a repeat/non-existent delete.
- Do not add a `.vN` version suffix to a **no-value** event subject (e.g. `…deleted`) — the version applies **only** when the event carries a value (create/update). Derive the version from the contract's `[Schema("vX.Y")]` major (default `.v1`); don't guess or hard-code it.
- Do not use `AssertWithValue` for a **no-value** event (Delete, or any `204 No Content`) — there is no returned value to reconstruct from. Use `AssertMetadata(destination, subject, key)` and pass the entity id (e.g. `2.ToGuid().ToString()`) as the `key` (the `.WithKey(id)` value). Reserve `AssertWithValue` for value-carrying Create/Update events.

## Further Reading

- [Testing Guide](https://github.com/Avanade/CoreEx/blob/main/samples/docs/testing.md) — full test architecture, data seeding, schema isolation, and E2E runner.
- [Pattern Catalog](https://github.com/Avanade/CoreEx/blob/main/samples/docs/patterns.md) — pattern catalog linking testing patterns to layer docs.
