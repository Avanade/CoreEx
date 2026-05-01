# CoreEx Capabilities & Patterns Guide

This document provides detailed explanations of CoreEx capabilities and common patterns to help developers understand the value and appropriate use cases for each feature. If you are deciding what to scaffold for a new service or domain, start with the [Application Scaffolding Guide](application-scaffolding-guide.md) and then use this document for the deeper capability details. If you are still learning how to ask the agent about these patterns effectively, also see the [Agent Interaction Guide](agent-interaction-guide.md) and [Agent Prompt Recipes](agent-prompt-recipes.md).

## Table of Contents

- [General Capabilities](#general-capabilities)
  - [Exception-Based Error Handling](#exception-based-error-handling)
  - [Dynamic Dependency Injection](#dynamic-dependency-injection)
  - [Entity Patterns](#entity-patterns)
  - [Roslyn Source Generation](#roslyn-source-generation)
  - [Instrumentation & Health Checks](#instrumentation--health-checks)
  - [Hybrid Caching (L1 + L2)](#hybrid-caching-l1--l2)
  - [Hosted Services](#hosted-services-timer--synchronized)
  - [Reference Data](#reference-data)
  - [JSON Filtering & Merge-Patch](#json-filtering--merge-patch)
  - [Validation](#validation)
  - [Mapping Helpers](#mapping-helpers)
  - [Globalization & Localization](#globalization--localization)
  - [Railway-Oriented Programming](#railway-oriented-programming-with-resultt)
- [API & HTTP Features](#api--http-features)
  - [Web API Styles](#web-api-styles-minimal--mvc)
  - [RFC 7386 Merge-Patch](#rfc-7386-merge-patch-applicationmerge-patchjson)
  - [Response JSON Filtering](#response-json-filtering)
  - [Error Handling with ProblemDetails](#error-handling-with-problemdetails)
  - [Conditional Request Semantics](#conditional-request-semantics-if-match)
  - [Idempotency-Key](#idempotency-key)
  - [Health Check Endpoints](#health-check-endpoints)
  - [OpenAPI Integration](#openapi-integration-nswag)
  - [CQRS](#cqrs-command-query-responsibility-segregation)
- [Data Access & Persistence](#data-access--persistence)
  - [Unit-of-Work with Integrated Outbox](#unit-of-work-with-integrated-outbox)
  - [Paging & Enumeration](#paging--enumeration)
  - [Dynamic Query](#dynamic-query-odata-style)
  - [Multi-Tenancy](#multi-tenancy)
  - [Type Discriminators](#type-discriminators)
- [Database Support](#database-support)
    - [SQL Server](#sql-server)
    - [PostgreSQL](#postgresql)
    - [ADO.NET Command & Parameter Extensions](#adonet-command--parameter-extensions)
    - [Entity Framework Integration](#entity-framework-integration)
- [Messaging & Events](#messaging--events)
    - [EventData Abstraction](#eventdata-abstraction)
    - [CloudEvent Interoperability](#cloudevent-interoperability)
    - [Publish + Subscribe Patterns](#publish--subscribe-patterns)
    - [Azure Service Bus Integration](#azure-service-bus-integration)
    - [Outbox Relay](#outbox-relay-with-partitioning)
    - [Workflow Orchestration (Durable Task SDK + DTS)](#workflow-orchestration-durable-task-sdk--dts)
- [Domain-Driven Design](#domain-driven-design)
    - [Aggregate & Entity Modeling](#aggregate--entity-modeling)
    - [Value Objects](#value-objects)
    - [Integration Events Only](#integration-events-only)
- [Putting It All Together](#putting-it-all-together-a-typical-request-flow)
- [Summary](#summary)

---

## General Capabilities

### Exception-Based Error Handling

**Pattern:** CoreEx defines specific exception types that map to HTTP status codes automatically.

CoreEx exception types include:
- `NotFoundException` — Resource not found (404).
- `ValidationException` — Validation failure (400).
- `ConcurrencyException` — ETag/version conflict (409).
- `BusinessException` — Domain rule violation (400).
- `AuthenticationException` — Unauthorized (401).
- `AuthorizationException` — Forbidden (403).

**Why it matters:** Instead of throwing generic `Exception` or returning error codes, you throw domain-specific exceptions that middlewares automatically convert to RFC 9457 ProblemDetails responses. This keeps error handling logic centralized and consistent across APIs.

**Example:**
```csharp
public async Task<Product> GetProductAsync(Guid id)
{
    var product = await _repository.GetByIdAsync(id);
    if (product == null)
        throw new NotFoundException($"Product '{id}' not found.");
    return product;
}

// Middleware automatically converts to:
// HTTP 404 with ProblemDetails JSON
```

### Dynamic Dependency Injection

**Pattern:** Register and resolve services without a traditional DI container through dynamic composition.

CoreEx provides extension methods like `AddExecutionContext()`, `AddMvcWebApi()`, `AddHttpWebApi()` that setup services with sensible defaults. You can layer additional registrations on top without heavyweight container configuration.

**Why it matters:** Reduces boilerplate, makes service composition explicit, and keeps middleware stacks clean and understandable.

**Example:**
```csharp
builder.Services
    .AddExecutionContext()           // Execution tenant/user context
    .AddMvcWebApi()                  // MVC + exception handling
    .AddHttpWebApi()                 // Minimal API + exception handling
    .AddSqlServerDatabase<DbContext>()
    .AddOutbox()
    .AddFusionCache();
```

### Entity Patterns

**Identifiers & Composite Keys**

CoreEx supports two patterns for entity identity:

1. **Single Identifier** — Most entities have a single ID (GUID, int, string).
   ```csharp
   public interface IIdentifier
   {
       object? Id { get; }
   }
   
   public class Product : IIdentifier
   {
       public Guid Id { get; set; }
       public string Sku { get; set; }
   }
   ```

2. **Composite Key** — Some entities have multi-part identity (e.g., tenant + entityId).
   ```csharp
   public interface ICompositeKey
   {
       object?[] CompositeKeys { get; }
   }
   
   public class TenantProduct : ICompositeKey
   {
       public Guid TenantId { get; set; }
       public Guid ProductId { get; set; }
       
       public object?[] CompositeKeys => new object[] { TenantId, ProductId };
   }
   ```

**ETags (Optimistic Concurrency)**

ETags prevent lost-update conflicts in optimistic concurrency scenarios:

```csharp
public interface IETag
{
    string? ETag { get; set; }
}

public class Product : IETag
{
    public Guid Id { get; set; }
    public string? ETag { get; set; }
    public decimal Price { get; set; }
}

// API usage:
// GET /api/products/123 returns Product with ETag: "abc123"
// PUT /api/products/123 with IF-MATCH: abc123 header
// If another request updated the product first, PUT returns 409 Conflict
```

**Change Logs (Audit Metadata)**

Track when entities were created and last modified:

```csharp
public interface IChangeLog
{
    ChangeLog? ChangeLog { get; set; }
}

public class ChangeLog
{
    public DateTime? CreatedDate { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public string? UpdatedBy { get; set; }
}

// Automatically populated by repository on insert/update
public class Product : IChangeLog
{
    public Guid Id { get; set; }
    public ChangeLog? ChangeLog { get; set; }
}
```

**Deep Compare**

Compare two entities for equality considering all properties recursively:

```csharp
var original = await _repo.GetByIdAsync(id);
// ... user modifies entity
var modified = new Product { Id = id, Name = "New Name", /* ... */ };

bool hasChanged = original.DeepEquals(modified);
if (!hasChanged) return NoContent(); // 204
```

### Roslyn Source Generation

**Pattern:** Auto-generate boilerplate code (e.g., serialization, mapping, contracts) at compile time using Roslyn analyzers.

CoreEx includes a contract generator that creates DTOs, mapping, and validation code from domain models using source generation. This eliminates manual mapping code and keeps serialization fast.

**Why it matters:** Reduces hand-written boilerplate, ensures domain model and contracts stay in sync, and improves startup performance via compile-time code generation.

### Instrumentation & Health Checks

**Pattern:** Built-in OpenTelemetry integration and standard health check endpoints.

CoreEx middleware automatically emits traces, metrics, and logs. Health checks are exposed on `/health/live` and `/health/ready` endpoints:

```csharp
app.MapHealthChecks("/health/live");  // Liveness (app running?)
app.MapHealthChecks("/health/ready"); // Readiness (ready to receive traffic?)
```

These endpoints integrate with Kubernetes and container orchestrators for probes and graceful shutdown.

### Hybrid Caching (L1 + L2)

**Pattern:** Distributed cache with local in-process backup for fault tolerance.

CoreEx uses **FusionCache** with optional **Redis** backplane:
- **L1:** In-process memory cache (fast, shared scope, ~1MB typical).
- **L2:** Redis (slower, shared across all service instances).
- **Fallback:** If Redis is down, L1 cache continues serving stale data.

```csharp
builder.Services.AddFusionCache()
    .WithRedisBackplane("localhost:6379");

// Usage in services:
var product = await _cache.GetOrSetAsync(
    key: $"product:{id}",
    factory: async ct => await _repository.GetByIdAsync(id, ct),
    duration: TimeSpan.FromHours(1),
    cancellationToken: ct
);
```

**Why it matters:** Dramatically improves performance (milliseconds vs. seconds), reduces database load, and handles Redis failures gracefully.

### Hosted Services (Timer & Synchronized)

**Pattern:** Background work scheduled at intervals or synchronized across multiple instances.

Use `IHostedService` implementation for:
- **Timer-based work** — Run a task every N seconds (e.g., cleanup, health checks).
- **Synchronized work** — Coordinate jobs across multiple instances using distributed locks.

```csharp
public class InventoryAdjustmentService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await AdjustInventoryAsync(stoppingToken);
        }
    }
}

builder.Services.AddHostedService<InventoryAdjustmentService>();
```

### Reference Data

**Pattern:** Load, cache, and orchestrate reference datasets (enums, lookup tables) with transactional integrity.

Reference data (like product categories, statuses, coupons) is typically read-heavy and must sync across services. CoreEx provides:

```csharp
// Define reference data
public class Category : ReferenceData
{
    public int Code { get; set; }
    public string? Description { get; set; }
}

// Load and cache
var categories = await _refData.GetCollectionAsync<Category>();

// Automatic caching with orchestration
// All instances see the same data
// Invalidation on source updates
```

**Why it matters:** Eliminates N+1 query problems, ensures consistency, and simplifies dependency management in distributed systems.

### JSON Filtering & Merge-Patch

**Pattern:** Dynamically exclude fields from responses and support RFC 7386 PATCH.

**Response Filtering** — Control which fields appear in JSON based on query parameters or roles:

```csharp
// GET /api/products/123?$fields=id,name
// Returns only id and name, omitting price, cost, margin
```

**Merge-Patch** — RFC 7386 PATCH for partial updates:

```csharp
// PATCH /api/products/123
// Content-Type: application/merge-patch+json
// {"name": "New Name"}  // other fields unchanged
```

Both use `System.Text.Json` without external dependencies.

### Validation

**Pattern:** Built-in validation rules as alternative to FluentValidation frameworks.

CoreEx provides validation decorators and APIs without forcing a particular framework:

```csharp
public class ProductValidator
{
    public static void Validate(Product p)
    {
        var errors = new List<string>();
        if (string.IsNullOrEmpty(p.Sku)) 
            errors.Add("Sku is required.");
        if (p.Price < 0)
            errors.Add("Price must be non-negative.");
        
        if (errors.Any())
            throw new ValidationException(errors);
    }
}
```

### Mapping Helpers

**Pattern:** Explicit one-way or bi-directional mapping without external frameworks.

CoreEx provides mapping utilities that make transformations explicit and traceable:

```csharp
var productDto = mapper.Map<ProductDto>(product);
// or bi-directional
var product = mapper.MapFrom<ProductDto>(dto);
```

No AutoMapper dependency means simpler dependencies and explicit code paths.

### Globalization & Localization

**Pattern:** Culture-aware text and formatting throughout requests.

`ExecutionContext` carries culture information per request:

```csharp
var currentCulture = ExecutionContext.Current?.CultureInfo; // e.g., "en-US"
var formattedPrice = product.Price.ToString("C", currentCulture);
```

Enables multi-language APIs without routing changes.

### Railway-Oriented Programming with Result<T>

**Pattern:** Composable error flow using `Result<T>` instead of exceptions for expected errors.

`Result<T>` represents success (Ok) or failure (Error) without throwing:

```csharp
public Result<Product> ValidateProduct(Product p)
{
    if (string.IsNullOrEmpty(p.Name))
        return Result<Product>.Error("Name is required.");
    
    return Result<Product>.Ok(p);
}

// Usage - chain results without try/catch
var result = ValidateProduct(product)
    .Then(p => _repository.SaveAsync(p))
    .Then(p => MapToDto(p));

if (!result.IsSuccessful)
    throw new ValidationException(result.Error);

return result.Value;
```

---

## API & HTTP Features

### Web API Styles (Minimal & MVC)

**Pattern:** Support both minimal APIs and MVC controllers with unified middleware.

CoreEx works with both styles seamlessly:

**Minimal API:**
```csharp
app.MapGet("/api/products/{id}", GetProduct)
   .WithName("GetProduct")
   .WithOpenApi();

async Task<ProductDto> GetProduct(Guid id, IProductService service) 
    => await service.GetProductAsync(id);
```

**MVC Controller:**
```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> GetProduct(Guid id)
        => await _service.GetProductAsync(id);
}
```

Both use the same exception handling, logging, and middleware.

### RFC 7386 Merge-Patch (application/merge-patch+json)

**Pattern:** Partial updates with semantic merge semantics.

Instead of PUT (must send whole resource) or ad-hoc PATCH, use standard merge-patch:

```csharp
// Partial update - unspecified fields unchanged
PATCH /api/products/123
Content-Type: application/merge-patch+json

{
  "name": "New Name"
  // price, category, etc. remain unchanged
}
```

Safer and more predictable than custom PATCH semantics.

### Response JSON Filtering

**Pattern:** Dynamically exclude fields from responses.

Reduces payload size and hides sensitive fields:

```csharp
// GET /api/products?$fields=id,name,price
// Response omits cost, margin, internalNotes

// GET /api/products?$fields=id,name
// Response omits all other fields
```

Implemented via middleware with zero manual code per endpoint.

### Error Handling with ProblemDetails

**Pattern:** RFC 9457 standard error responses everywhere.

CoreEx exception middleware automatically converts exceptions to ProblemDetails:

```json
{
  "type": "https://example.com/problems/not-found",
  "title": "Not Found",
  "status": 404,
  "detail": "Product 'xyz' not found.",
  "traceId": "0HN4..."
}
```

Consistent error format across all endpoints and all APIs.

### Conditional Request Semantics (IF-MATCH)

**Pattern:** Prevent lost updates and concurrent modifications via ETags.

GET returns an ETag; PUT/PATCH require IF-MATCH header:

```http
GET /api/products/123
200 OK
ETag: "v2-abc123"

{product json}

---

PUT /api/products/123
IF-MATCH: v2-abc123
{updated fields}
200 OK

---

// If stale:
PUT /api/products/123
IF-MATCH: v1-old
{updated fields}
409 Conflict  // Another request updated it
```

Prevents lost-update anomalies in concurrent scenarios.

### Idempotency-Key

**Pattern:** Automatic deduplication of POST operations.

Clients send a unique `Idempotency-Key` header; CoreEx ensures the operation runs exactly once:

```http
POST /api/baskets/123/checkout
Idempotency-Key: client-request-id-abc123

201 Created / 200 OK
```

If the same key is resubmitted, CoreEx returns the cached response without re-executing.

**Why it matters:** Safe retries in unreliable networks; critical for payment systems, order placement, etc.

### Health Check Endpoints

**Pattern:** Expose `/health/live` and `/health/ready` for orchestration.

Kubernetes, Docker Swarm, and load balancers probe these endpoints:

```
GET /health/live
200 OK  (app is running)

GET /health/ready
503 Service Unavailable  (database down, not ready for traffic)
```

Typical ready checks include database connectivity, cache availability, and broker connectivity.

### OpenAPI Integration (NSwag)

**Pattern:** Auto-generate OpenAPI schemas for use in Swagger UI and clients.

CoreEx integrates with NSwag to produce accurate OpenAPI 3.0+ schemas:

```csharp
builder.Services.AddOpenApiDocument(opts =>
{
    opts.Title = "Product API";
    opts.Version = "v1";
});

app.UseOpenApi();                       // Serves /swagger/v1/openapi.json
app.UseSwaggerUI();                     // Serves Swagger UI
```

**Why it matters:** Automatically generated API docs that stay in sync; enables client code generation.

### CQRS (Command Query Responsibility Segregation)

**Pattern:** Separate read and write services when architectures demand it.

Typical microservice uses a single domain model. For complex systems:

- **Commands (Write)** — ProductMutationService handles create/update/delete.
- **Queries (Read)** — ProductQueryService handles all reads with separate caching.

```csharp
// Write model
public class ProductMutationService
{
    public async Task<Product> CreateAsync(CreateProductRequest req) { ... }
    public async Task UpdateAsync(Guid id, UpdateProductRequest req) { ... }
}

// Read model
public class ProductQueryService
{
    public async Task<Product> GetAsync(Guid id) { ... }
    public async Task<IEnumerable<Product>> QueryAsync(FilterOptions opts) { ... }
}
```

Useful for event-sourced or high-scale systems; adds complexity otherwise.

---

## Data Access & Persistence

### Unit-of-Work with Integrated Outbox

**Pattern:** Transactional boundary ensuring database writes and event publishing are atomic.

The unit-of-work wraps all database operations and maintains an outbox table for events:

```csharp
public async Task CreateProductAsync(CreateProductRequest req)
{
    using var uow = _unitOfWorkFactory.Create();
    
    var product = new Product { Name = req.Name, Price = req.Price };
    await uow.Products.SaveAsync(product);
    
    // Event added to UoW, written to [Products].[Outbox]
    uow.Events.Add(new ProductCreated { ProductId = product.Id });
    
    // All database writes flushed atomically
    await uow.CommitAsync();
    
    // Separate relay process reads [Products].[Outbox] 
    // and publishes to Service Bus
}
```

**Why it matters:** If you crash after committing to the database, events are guaranteed to be published (via relay). Eliminates the dual-write problem.

### Paging & Enumeration

**Pattern:** Skip/take pagination with total count for OData-like APIs.

Pagination is stateless and works with dynamic filtering:

```csharp
public class PagingArgs
{
    public int Skip { get; set; }  // 0-based offset
    public int Take { get; set; }  // page size, usually 10–100
}

public async Task<(IEnumerable<Product>, long TotalCount)> QueryAsync(
    PagingArgs paging, 
    FilterOptions? filter = null)
{
    var products = await _repository.QueryAsync(paging, filter);
    var totalCount = await _repository.CountAsync(filter);
    return (products, totalCount);
}

// HTTP usage:
// GET /api/products?$skip=0&$take=20
// Returns 20 products + X-Total-Count: 1543 header
```

### Dynamic Query (OData-Style)

**Pattern:** User-provided filtering and ordering without hardcoding every combination.

CoreEx translates query parameters to SQL dynamically:

```
GET /api/products?$filter=price gt 100 and category eq 'Bikes'&$orderby=name&$fields=id,name,price
```

Supports:
- Comparison operators: `eq`, `ne`, `gt`, `ge`, `lt`, `le`
- Logical operators: `and`, `or`
- Functions: `contains`, `startswith`, `endswith`
- Ordering: `$orderby=field1,field2 desc`
- Projection: `$fields=id,name` (response filtering)

### Multi-Tenancy

**Pattern:** Isolate data per tenant transparently via `ExecutionContext`.

Each request carries tenant identity in `ExecutionContext`:

```csharp
var tenantId = ExecutionContext.Current?.TenantId;

// Repositories automatically filter by tenant
var products = await _repository.QueryAsync(); // Only this tenant's products
```

Database rows include a `TenantId` column; queries are filtered in the WHERE clause automatically.

### Type Discriminators

**Pattern:** Model polymorphic or partitioned data sets using discriminator columns.

When entities might be subtypes (e.g., `Product` might be `PhysicalProduct` or `DigitalProduct`):

```csharp
public abstract class Product
{
    public Guid Id { get; set; }
    public string Type { get; set; }  // Discriminator
}

public class PhysicalProduct : Product
{
    public decimal Weight { get; set; }
    public string Dimensions { get; set; }
}

public class DigitalProduct : Product
{
    public Uri DownloadUrl { get; set; }
    public int MaxDownloads { get; set; }
}
```

Stored in one table with a `Type` column; ORM automatically hydrates correct subclass.

---

## Database Support

### SQL Server

**Pattern:** Primary database target with full feature support.

CoreEx ships with `CoreEx.Database.SqlServer` providing:
- Migrations via **DbEx** (custom migration runner).
- Data seeding from YAML files.
- Outbox relay with partitioning.
- Full TSQL support.

In this repository, SQL Server is the **default initial implementation** and the most complete scaffolding target. That reflects current sample coverage and tooling depth, not a claim that CoreEx patterns are inherently SQL Server-only.

### PostgreSQL

**Pattern:** Secondary/evolving support.

PostgreSQL support depends on the package (marked with `*` in documentation). Many CoreEx features work, but SQL Server is the first-class target.

### ADO.NET Command & Parameter Extensions

**Pattern:** Fluent ADO.NET helpers reduce boilerplate SQL composition.

Instead of manual `SqlCommand` construction:

```csharp
// Manual
var cmd = new SqlCommand("SELECT * FROM [Products] WHERE Id = @Id", connection);
cmd.Parameters.AddWithValue("@Id", id);

// CoreEx extension
var cmd = connection.CreateCommand("SELECT * FROM [Products] WHERE Id = @Id")
    .ParamWithValue("@Id", id);
```

Safer, more readable, less repetitive.

### Entity Framework Integration

**Pattern:** CoreEx works with EF Core repositories and unit-of-work patterns.

`CoreEx.EntityFrameworkCore` provides:
- Base repository classes wrapping `DbSet<T>`.
- Unit-of-work with EF's `SaveChangesAsync()`.
- Outbox integration.

```csharp
public class ProductRepository : Repository<Product>
{
    public ProductRepository(DbContext context) : base(context) { }
    
    public async Task<Product?> GetBySkuAsync(string sku)
        => await _context.Products.SingleOrDefaultAsync(p => p.Sku == sku);
}
```

---

## Messaging & Events

### EventData Abstraction

**Pattern:** Format-agnostic event envelope that decouples event definition from transport.

Events are serialized into `EventData` and can be published to any broker:

```csharp
public class ProductCreatedEvent
{
    public Guid ProductId { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Wrapped in EventData
var eventData = new EventData
{
    Subject = "contoso.products.product",
    Action = "created",
    Version = "v1",
    Data = JsonSerializer.SerializeToElement(new ProductCreatedEvent { ... })
};

// Can publish to Service Bus, RabbitMQ, Kafka, etc.
await _eventPublisher.PublishAsync(eventData);
```

### CloudEvent Interoperability

**Pattern:** Automatic conversion to CNCF CloudEvents format.

`EventData` can be serialized/deserialized as CloudEvents for standards compliance:

```json
{
  "specversion": "1.0",
  "type": "com.example.products.created",
  "source": "https://example.com/products",
  "id": "abc123",
  "time": "2024-01-15T12:34:56Z",
  "data": { "productId": "xyz", "name": "Bike" }
}
```

Enables interop with other CloudEvents consumers (AWS EventBridge, etc.).

### Publish + Subscribe Patterns

**Pattern:** Per-message subscription with configurable consumption strategy.

Subscribers join a topic/queue and consume messages from a specific position:

- **From Beginning** — Consume all historical events.
- **From End** — Consume only new events from now on.
- **Latest Checkpoint** — Resume from where the subscriber last left off.

```csharp
public class ProductModifySubscriber : SubscriberHost<ProductCreatedEvent>
{
    protected override async Task OnEventAsync(
        EventData eventData, 
        ProductCreatedEvent data,
        CancellationToken cancellationToken)
    {
        // React to product creation
        // E.g., sync to Read Model, update search index
        await _searchIndex.IndexAsync(data.ProductId, cancellationToken);
    }
}
```

### Azure Service Bus Integration

**Pattern:** Native Service Bus topic/subscription support with partitioning.

CoreEx provides `IEventPublisher` and `ISubscriber` implementations for Service Bus:

```csharp
builder.Services.AddServiceBusEventPublisher("Endpoint=...");
builder.Services.AddServiceBusSubscriber<ProductCreatedEvent>("products");
```

Handles:
- Topic/subscription creation.
- Automatic serialization.
- Partition affinity (partition key = session ID for ordered processing).

In this repository, Azure Service Bus is the **default initial broker implementation** because the sample relay/subscriber hosts are wired around it. The surrounding event model is broader than that specific broker choice: `EventData` is transport-oriented rather than Service Bus-specific.

### Outbox Relay (with Partitioning)

**Pattern:** Dedicated host that reads from database outbox and publishes to broker.

Each domain has its own relay process:

1. Business logic writes events to `[Schema].[Outbox]` table within transaction.
2. Separate **Outbox.Relay** host polls the table every N seconds.
3. Relay fetches unpublished outbox rows and publishes to Service Bus.
4. On success, marks rows as published.
5. On failure, retries with exponential backoff.

```
┌─────────────────┐
│   API Process   │
│  Writes events  │
│   to Outbox     │
└────────┬────────┘
         │
    [DB Outbox]
         │
┌────────▼────────┐
│ Outbox.Relay    │
│   Polls every   │
│   5 seconds     │
└────────┬────────┘
         │
    [Service Bus]
         │
┌────────▼───────────┐
│ Subscribe Services │
│  React to events   │
└────────────────────┘
```

**Partitioning:** If an event has a `PartitionKey`, relay publishes to the same partition in Service Bus for ordered processing.

**Why it matters:** Guarantees events are published even if relay crashes; decouples API availability from messaging; enables ordered processing of related events.

### Workflow Orchestration (Durable Task SDK + DTS)

**Pattern:** Durable workflow coordination for long-running, stateful, and business-critical process flows.

Use orchestration when a process needs one or more of these characteristics:

- Long-running steps that must survive restarts.
- Fan-out or fan-in aggregation across parallel work items.
- Batch processing with retries and controlled concurrency.
- Compensation paths when downstream operations fail.
- External-event waits, timers, and human-approval checkpoints.
- Full execution audit trail and replay semantics.

CoreEx samples include orchestration hosted in standard .NET worker processes using the Durable Task SDK with a DTS backend, including local emulator support and containerized hosting alignment.

See [Orchestration with the Durable Task SDK](orchestration.md) for detailed guidance and examples.

---

## Domain-Driven Design

### Aggregate & Entity Modeling

**Pattern:** Implement aggregates as root objects that enforce invariants and encapsulate child entities.

An **aggregate root** orchestrates its entities and ensures consistency:

```csharp
public class Basket : IIdentifier, IETag, IChangeLog
{
    public Guid Id { get; set; }
    public string? ETag { get; set; }
    public ChangeLog? ChangeLog { get; set; }
    
    public Guid CustomerId { get; set; }
    public string StatusCode { get; set; } = "Active";
    
    // Child entities
    private List<BasketItem> _items = new();
    public IReadOnlyList<BasketItem> Items => _items.AsReadOnly();
    
    // Business rules
    public void AddItem(Guid productId, int quantity)
    {
        if (StatusCode != "Active")
            throw new BusinessException("Cannot add item to checked-out basket.");
        
        var existing = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existing != null)
            existing.Quantity += quantity;
        else
            _items.Add(new BasketItem { ProductId = productId, Quantity = quantity });
    }
    
    public void Checkout()
    {
        if (!_items.Any())
            throw new ValidationException("Cannot checkout empty basket.");
        
        StatusCode = "CheckedOut";
    }
}

public class BasketItem // Child entity, not an aggregate root
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}
```

Aggregate roots:
- Own their entities (users modify through the root).
- Enforce invariants (business rules).
- Publish integration events.
- Are the transactional boundary.

### Value Objects

**Pattern:** Implement as immutable C# record classes with semantic equality.

Value objects have no identity, only their values matter:

```csharp
public record ItemPricing(
    string UnitOfMeasure,
    int Quantity,
    decimal UnitPrice)
{
    public decimal Total => Quantity * UnitPrice;
}

// Usage
var pricing = new ItemPricing("ea", 5, 19.99m);

// Equality is by value
var pricing2 = new ItemPricing("ea", 5, 19.99m);
Assert.AreEqual(pricing, pricing2); // True

// Immutable
// pricing.Quantity = 10;  // CS8852: Init-only property
```

Record classes automatically provide:
- Value-based equality (`Equals`, `GetHashCode`).
- `ToString()` for debugging.
- Deconstruction.

### Integration Events Only

**Pattern:** Focus on integration events (published to outbox/broker) rather than domain events (in-process messaging).

**Integration Events** — Published to an external event broker; subscribers in other services react.
```csharp
public class ProductCreatedIntegrationEvent
{
    public Guid ProductId { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Published to Service Bus for other services to consume
await _unitOfWork.Events.Add(new EventData { ... });
```

**Avoid:** Domain Events + MediatR for in-process messaging.
```csharp
// NOT recommended in CoreEx style
public class ProductCreatedDomainEvent { ... }
_mediator.Publish(new ProductCreatedDomainEvent(...)); // In-process
```

**Why:** Keep services decoupled and independent. If you need cross-domain orchestration, use integration events and let services react asynchronously.

---

## Putting It All Together: A Typical Request Flow

To illustrate how these patterns work together, here's a typical request flow based on the **Contoso sample architecture in this repository**, not a mandatory flow for every CoreEx application.

This example assumes the samples' **full outboxing and messaging setup**:

- an API host handling the request
- a database-backed unit-of-work writing to an outbox table
- a separate `Outbox.Relay` host publishing to Azure Service Bus
- another service consuming the resulting integration event

```
1. Client: POST /api/products
   with Idempotency-Key header

2. CoreEx Middleware:
   - Extract ExecutionContext (tenant, user, culture)
   - Check Idempotency-Key (cached response if duplicate)
   - Route to controller

3. ProductController:
   - Validate input (ValidationException if invalid)
   - Call ProductService

4. ProductService:
   - Create Product entity
   - Apply domain rules (throw BusinessException if violated)
   - Create UnitOfWork
   - Save to repository (within transaction)
   - Add integration event to UoW.Events
   - Call UoW.CommitAsync() — atomically saves product + event to Outbox

5. Repository:
   - Execute INSERT on [Products].[Product]
   - Add row to [Products].[Outbox]
   - Assign ETag, ChangeLog
   - Transaction commits

6. Separate Outbox.Relay Process:
   - Poll [Products].[Outbox] every N seconds
   - Find unpublished events
   - Publish to Service Bus
   - Mark as published

7. Other Services Subscribe:
   - Shopping.Subscribe consumes ProductCreated event
   - Syncs product replica to [Shopping].[Product]

8. CoreEx Response Handler:
   - Convert Product to ProductDto (response filtering)
   - Apply $fields projection
   - Return 201 Created
   - Include ETag header and Location header

9. Client:
   - Receives 201 with ETag
   - For future updates, uses IF-MATCH: {ETag} header
```

---

## Summary

CoreEx provides a cohesive set of patterns and utilities that work together to enable:

- **Consistent API behavior** across minimal APIs and MVC.
- **Reliable messaging** via transactional outboxes.
- **Durable workflow orchestration** for long-running, compensating, and replayable process flows.
- **Multi-tenancy** and **concurrency** handling built-in.
- **Event-driven architecture** with integration events.
- **Clear separation of concerns** (aggregates, value objects, services).
- **Type-safe operations** (exceptions, Result types, source generation).

The framework is particularly well-suited for distributed microservices architectures where consistency, reliability, and maintainability are critical.
