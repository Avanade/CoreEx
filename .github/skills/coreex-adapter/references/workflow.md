# coreex-adapter: Workflow

Full workflow for adding or modifying an adapter (anti-corruption layer) in a CoreEx `*.Application` + `*.Infrastructure` host pair.

---

## Phase 1 — Clarify Before Writing

| Question | Default | Notes |
|---|---|---|
| External system name | — | Used as sub-folder name. Can be a domain (`Products`, `Orders`), a system type (`Idp`, `Erp`), or a product name (`Stripe`, `Salesforce`). Choose a name that reflects the boundary, not the transport. |
| Adapter role | Synchronous | Synchronous = real-time HTTP call. Replication = event-driven local store sync. |
| HTTP calls needed? | Yes | No HTTP = EF-only replication adapter; skip HTTP client steps. |
| Multiple endpoints from the same system? | Yes | Always use sub-folder — it is the standard regardless of endpoint count. |
| Needs local EF read alongside HTTP? | Ask | Adapter may combine local replicated data + live HTTP (see `ProductAdapter`). |
| Unit tests for HTTP client? | Yes for any HTTP | Test each distinct response code the service depends on. |

---

## Step 1 — Application-Layer Interface

Create one interface per adapter role in `Application/Adapters/{ExternalSystem}/`. The sub-folder name reflects the external system boundary — it may be a domain (`Products`), a system type (`Idp`, `Erp`), or a product name (`Stripe`, `Salesforce`).

### Synchronous adapter (real-time calls)

```csharp
// Application/Adapters/Products/IProductAdapter.cs
namespace {Domain}.Application.Adapters.Products;

/// <summary>
/// Enables the Products domain integration, serving as the external dependency boundary
/// (anti-corruption layer) for product-related operations.
/// </summary>
public interface IProductAdapter
{
    Task<Result<Product>> GetAsync(string id, CancellationToken ct = default);
    Task<Result> ReserveInventoryAsync(Domain.Basket basket, CancellationToken ct = default);
    Task<Result> CancelReservationAsync(Domain.Basket basket, CancellationToken ct = default);
}
```

### Replication adapter (event-driven local sync)

```csharp
// Application/Adapters/Products/IProductSyncAdapter.cs
namespace {Domain}.Application.Adapters.Products;

/// <summary>
/// Enables Products-domain data synchronization via event-based subscriptions.
/// </summary>
public interface IProductSyncAdapter
{
    Task<Result> ModifyAsync(Product product, CancellationToken ct = default);
    Task<Result> DeleteAsync(string id, CancellationToken ct = default);
}
```

**Rules:**
- Interface name follows the calling domain's language, not the remote API's naming.
- All methods return `Result` or `Result<T>` — no plain values, no thrown exceptions at the interface boundary.
- One interface file per role (synchronous vs replication); do not combine them.

---

## Step 2 — Typed HTTP Client (if HTTP calls needed)

Create in `Infrastructure/Clients/{ExternalDomain}/`. One class per external service; one method per remote endpoint.

### Client class

```csharp
// Infrastructure/Clients/Products/ProductsHttpClient.cs
namespace {Domain}.Infrastructure.Clients.Products;

/// <summary>
/// Provides the HTTP facade for interacting with the external Products API.
/// </summary>
public class ProductsHttpClient(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient.ThrowIfNull();

    /// <summary>Creates a new inventory reservation.</summary>
    public async Task<Result> CreateReservationAsync(MovementRequest request, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/inventory/reserve", request, JsonDefaults.SerializerOptions, ct).ConfigureAwait(false);
        return await response.ToResultAsync(ct).ConfigureAwait(false);
    }
}
```

### Local request/response DTOs

Keep HTTP-transport DTOs in the same `Clients/{ExternalDomain}/` sub-folder — they are infrastructure concerns, invisible to the Application layer.

```csharp
// Infrastructure/Clients/Products/MovementRequest.cs
namespace {Domain}.Infrastructure.Clients.Products;

public class MovementRequest : IIdentifier<string?>
{
    public string? Id { get; set; }
    public DataMap<MovementRequestProduct>? Products { get; set; }
}

// Infrastructure/Clients/Products/MovementRequestProduct.cs
namespace {Domain}.Infrastructure.Clients.Products;

public class MovementRequestProduct
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public decimal Quantity { get; set; }
    public string? UnitOfMeasure { get; set; }
}
```

**Rules:**
- No `[ScopedService]` on the HTTP client — it is registered via `AddTypedHttpClient<T>`.
- `response.ToResultAsync()` handles all success/error mapping — never call `response.EnsureSuccessStatusCode()` directly.
- DTOs in `Clients/{ExternalDomain}/` are only referenced from within the Infrastructure layer.

---

## Step 3 — Adapter Implementation

Create in `Infrastructure/Adapters/{ExternalDomain}/`. Implement the Application interface; register with `[ScopedService<IXxxAdapter>]`.

### Synchronous adapter (HTTP + local EF + event publication)

```csharp
// Infrastructure/Adapters/Products/ProductAdapter.cs
namespace {Domain}.Infrastructure.Adapters.Products;

[ScopedService<IProductAdapter>]
public class ProductAdapter({Domain}EfDb ef, IEventPublisher eventPublisher, ProductsHttpClient client, [FromKeyedServices("AzureServiceBus")] IEventPublisher serviceBusPublisher) : IProductAdapter
{
    private readonly {Domain}EfDb _ef = ef.ThrowIfNull();
    private readonly IEventPublisher _eventPublisher = eventPublisher.ThrowIfNull();
    private readonly ProductsHttpClient _client = client.ThrowIfNull();
    private readonly IEventPublisher _serviceBusPublisher = serviceBusPublisher.ThrowIfNull();

    /// <inheritdoc/>
    /// <remarks>Leverages the internal event-based replication store.</remarks>
    public Task<Result<Product>> GetAsync(string id, CancellationToken ct = default)
        => Result.GoAsync(() => _ef.Products.GetWithResultAsync(id, ct))
                 .ThenAs(p => ProductMapper.From.Map(p));

    /// <inheritdoc/>
    /// <remarks>Invokes the Products API directly (real-time) to perform reservation.</remarks>
    public async Task<Result> ReserveInventoryAsync(Domain.Basket basket, CancellationToken ct = default)
    {
        // Filter to stocked products only — non-stocked items do not need reservation.
        var ids = basket.Items.Select(i => i.ProductId).ToArray();
        ids = await _ef.Products.Query().Where(p => ids.Contains(p.Id!) && !p.IsNonStocked).Select(p => p.Id!).ToArrayAsync(ct).ConfigureAwait(false);

        if (ids.Length == 0)
            return Result.Success;

        var req = new MovementRequest
        {
            Id = basket.Id,
            Products = basket.Items.Where(i => ids.Contains(i.ProductId)).ToDataMap(
                x => x.ProductId,
                x => new MovementRequestProduct { Quantity = x.Pricing.Quantity, UnitOfMeasure = x.Pricing.UnitOfMeasure })
        };

        return await _client.CreateReservationAsync(req, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    /// <remarks>Sends the cancel command directly via the broker, bypassing the Outbox — used
    /// only in the checkout failure compensation path where the Outbox may itself have failed.</remarks>
    public Task<Result> CancelReservationAsync(Domain.Basket basket, CancellationToken ct = default)
    {
        _serviceBusPublisher.Add(EventData.CreateCommand("products", "reservation", "cancel").WithKey(basket.Id));
        return Result.GoAsync(() => _serviceBusPublisher.PublishAsync(CancellationToken.None));
    }
}
```

### Replication adapter (EF upsert/delete only)

```csharp
// Infrastructure/Adapters/Products/ProductSyncAdapter.cs
namespace {Domain}.Infrastructure.Adapters.Products;

[ScopedService<IProductSyncAdapter>]
public class ProductSyncAdapter({Domain}EfDb ef) : IProductSyncAdapter
{
    private readonly {Domain}EfDb _ef = ef.ThrowIfNull();

    /// <inheritdoc/>
    public async Task<Result> ModifyAsync(Product product, CancellationToken ct = default)
        => (await _ef.Products.UpsertWithResultAsync(ProductMapper.To.Map(product), ct).ConfigureAwait(false)).AsResult();

    /// <inheritdoc/>
    public async Task<Result> DeleteAsync(string id, CancellationToken ct = default)
        => (await _ef.Products.DeleteWithResultAsync(id, ct).ConfigureAwait(false)).AsResult();
}
```

**Rules:**
- `[ScopedService<IXxxAdapter>]` on every implementation — picked up by `AddDynamicServicesUsing<T>()` in `Program.cs`.
- Never call `HttpClient` directly in the adapter — always delegate to the typed client class.
- Use `CancellationToken.None` (not the request `ct`) for **compensation paths** (e.g., rolling back a reservation after a checkout failure) — the request token may already be cancelled and must not abort the compensation.
- Event publication inside a `TransactionAsync` uses the transactional `IEventPublisher` (Outbox). Bypass-Outbox paths use the Service Bus publisher directly and call `PublishAsync()` explicitly.

---

## Step 4 — Registration

### Typed HTTP client (Program.cs)

```csharp
// Program.cs — in the services configuration block
builder.AddTypedHttpClient<ProductsHttpClient>("ProductsApi");
```

The `"ProductsApi"` name must match the name used in `MockHttpClientFactory.CreateClient("ProductsApi", ...)` in tests.

**Configuration** (appsettings.json / appsettings.Development.json):

```json
"Aspire": {
  "HttpClient": {
    "ProductsApi": {
      "ServiceUri": "https://localhost:5001"
    }
  }
}
```

### Adapter auto-discovery

The adapter is registered automatically via `[ScopedService<IXxxAdapter>]` — no explicit `AddScoped` call needed provided `AddDynamicServicesUsing<T>()` is configured in `Program.cs`.

---

## Step 5 — Unit Tests for the HTTP Client

Unit-test every distinct status code that the consuming service acts on. Use `WithGenericTester<EntryPoint>` from UnitTestEx — the same `MockHttpClientFactory` pattern used in API integration tests, but backed by the lightweight unit-test host.

### EntryPoint — register the typed HTTP client

The unit-test `EntryPoint` must register the typed HTTP client so it resolves from DI:

```csharp
// Test.Unit/EntryPoint.cs
public class EntryPoint
{
    public static void ConfigureApplication(IHostApplicationBuilder builder)
    {
        builder.Services.AddExecutionContext();
        // Register the typed HTTP client — required so tests can resolve it from DI.
        builder.AddTypedHttpClient<{External}HttpClient>("{ExternalService}");
    }
}
```

### Test class

```csharp
// Tests/Clients/{External}/{External}HttpClientTests.cs
namespace {Domain}.Test.Unit.Clients.{External};

public class {External}HttpClientTests : WithGenericTester<EntryPoint>
{
    private UnitTestEx.Mocking.MockHttpClientRequest _mockHttpReserveRequest = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        // Use fully-qualified UnitTestEx.MockHttpClientFactory.Create() to resolve the ambiguity
        // with UnitTestEx.Mocking.MockHttpClientFactory when both namespaces are in GlobalUsing.
        var mcf = UnitTestEx.MockHttpClientFactory.Create();
        // CreateClient without a base URI — uses the address registered in DI/config.
        _mockHttpReserveRequest = mcf.CreateClient("{ExternalService}").Request(HttpMethod.Post, "api/{resource}");
        Test.ReplaceHttpClientFactory(mcf);
    }

    [Test]
    public void CreateAsync_Success_ReturnsSuccess() => Test.Scoped(async test =>
    {
        _mockHttpReserveRequest.WithAnyBody()
            .Respond.With(HttpStatusCode.NoContent);

        test.Run(async _ =>
        {
            var client = ExecutionContext.GetRequiredService<{External}HttpClient>();
            var result = await client.CreateAsync(new {External}Request { Id = "test-1" }).ConfigureAwait(false);
            result.IsSuccess.Should().BeTrue();
        });

        _mockHttpReserveRequest.Verify();
    });

    [Test]
    public void CreateAsync_ServerError_ReturnsFailure() => Test.Scoped(async test =>
    {
        _mockHttpReserveRequest.WithAnyBody()
            .Respond.With(HttpStatusCode.InternalServerError);

        test.Run(async _ =>
        {
            var client = ExecutionContext.GetRequiredService<{External}HttpClient>();
            var result = await client.CreateAsync(new {External}Request { Id = "test-1" }).ConfigureAwait(false);
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<HttpRequestException>();
        });

        _mockHttpReserveRequest.Verify();
    });

    [Test]
    public void CreateAsync_BusinessError_ReturnsBusinessFailure() => Test.Scoped(async test =>
    {
        _mockHttpReserveRequest.WithAnyBody()
            .Respond.WithJson(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                title = "Business Error",
                status = 422,
                code = "UnprocessableEntity"
            }, HttpStatusCode.UnprocessableContent, "application/problem+json");

        test.Run(async _ =>
        {
            var client = ExecutionContext.GetRequiredService<{External}HttpClient>();
            var result = await client.CreateAsync(new {External}Request { Id = "test-1" }).ConfigureAwait(false);
            result.IsFailure.Should().BeTrue();
        });

        _mockHttpReserveRequest.Verify();
    });
}
```

**Key patterns:**
- `WithGenericTester<EntryPoint>` — lightweight DI host; mirrors the `WithApiTester<Program>` pattern used in API integration tests
- `[OneTimeSetUp]` creates the factory once and wires it into the host via `Test.ReplaceHttpClientFactory(mcf)` — the same approach used in API/mutate tests
- `mcf.CreateClient("{ExternalService}")` without a base URI — uses the address registered in config, not a hardcoded test URI
- Store the request mock as a field (`_mockHttpReserveRequest`) — shared across tests; set the response per-test via `WithAnyBody().Respond.With(...)`
- `Test.Scoped(async test => { ... })` + `test.Run(async _ => { ... })` — proper UnitTestEx scoped execution; tests are `void`, not `async Task`
- `ExecutionContext.GetRequiredService<{External}HttpClient>()` — resolves the client from DI; do not `new` it directly

**When to write HTTP client unit tests vs rely on integration tests:**

| Scenario | Test approach |
|---|---|
| HTTP client with multiple endpoints or complex request shaping | Unit-test each endpoint/status code with `WithGenericTester` |
| Simple adapter with only EF reads/writes (no HTTP) | Integration tests cover this via intra-domain service tests |
| Adapter orchestration (combining EF + HTTP + events) | Mock the HTTP client via `Test.ReplaceHttpClientFactory(mcf)` in `WithApiTester` integration tests |

---

## Guardrails

- **Sub-folder is always required** — never put adapter or client files directly in `Adapters/` or `Clients/` without a domain sub-folder
- **Interface surface is domain-idiomatic** — method names and parameter types use your domain's language, not the remote API's
- **No `HttpClient` in adapter methods** — the adapter delegates to the typed client; the typed client owns the HTTP concern
- **`response.ToResultAsync()`** — always use this; never call `EnsureSuccessStatusCode()`; never swallow non-success responses silently
- **`CancellationToken.None` in compensation paths** — a cancelled request token must never abort a compensation operation (e.g., rolling back a reservation after a failed checkout)
- **Two separate interface files** for synchronous vs replication roles — do not combine them into one interface
- **Local DTOs stay in `Clients/{ExternalDomain}/`** — never expose them in the Application layer; map at the adapter boundary
- **`[ScopedService<IXxxAdapter>]`** on the implementation — never use `[SingletonService]` or `[TransientService]` for adapters
