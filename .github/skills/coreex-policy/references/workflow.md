# coreex-policy: Workflow

Full workflow for creating or modifying a CoreEx Application-layer policy class in `Application/Policies/`. A policy encapsulates guard logic that requires async I/O and cannot live in a synchronous validator.

---

## Phase 1 — Clarify Before Writing

| Question | Default | Notes |
|---|---|---|
| What entity is guarded? | Ask | Names the policy and its methods |
| Guard type? | EnsureExists | EnsureExists / business rule / state check |
| Returns entity or pass/fail? | Returns entity | `Result<Contracts.T>` lets callers reuse loaded data; `Result` for pure pass/fail |
| Adapter or repository dependency? | Ask | Inject whatever is already available in the calling service |
| Multiple guard methods in one class? | No | Group when they share the same dependency |

---

## Step 1 — Scaffold

Create `Application/Policies/{Name}Policy.cs`:

```csharp
namespace {Solution}.Application.Policies;

public class {Name}Policy(I{Dep}Adapter {dep}Adapter)
{
    private readonly I{Dep}Adapter _{dep}Adapter = {dep}Adapter.ThrowIfNull();
}
```

**Only accept dependencies already injected into the calling service.** The policy is instantiated at the call site — not registered in DI — so its constructor arguments must be fields the service already holds.

When the policy depends on a repository instead of an adapter:

```csharp
public class {Name}Policy(I{Name}Repository repository)
{
    private readonly I{Name}Repository _repository = repository.ThrowIfNull();
}
```

---

## Step 2 — EnsureExists Guard (most common)

Fetch the entity via the adapter. Translate `NotFoundError` into a user-visible field-level validation error so the caller sees a clean error at the referencing field, not a raw 404.

### Returns the entity (`Result<T>`) — preferred when caller needs the loaded value

```csharp
public Task<Result<Contracts.{Name}>> EnsureExistsAsync(string id) => Result
    .GoAsync(() => _{dep}Adapter.GetAsync(id))
    .OnFailure(r => r.IsNotFoundError
        ? Result.ValidationError(MessageItem.CreateErrorMessage(nameof(id), "{Name} was not found."))
        : r);
```

`OnFailure` runs only when the result is a failure. The ternary re-maps `NotFoundError` to a validation error; any other failure kind propagates unchanged (the `: r` branch).

### Returns pass/fail only (`Result`) — when caller does not need the entity

```csharp
public async Task<Result> EnsureExistsAsync(string id)
{
    var r = await _{dep}Adapter.GetAsync(id).ConfigureAwait(false);
    return r.IsFailure
        ? (r.IsNotFoundError
            ? Result.ValidationError(MessageItem.CreateErrorMessage(nameof(id), "{Name} was not found."))
            : r.AsResult())
        : Result.Success;
}
```

---

## Step 3 — Business Rule Guard

Guard a state condition on the loaded entity. Return `Result.BusinessError` when the rule is violated.

```csharp
public async Task<Result> EnsureActiveAsync(string id)
{
    var r = await _{dep}Adapter.GetAsync(id).ConfigureAwait(false);
    if (r.IsFailure)
        return r.AsResult();

    if (r.Value.IsInactive)
        return Result.BusinessError("{Name} is inactive.", e => e.WithErrorCode("{name}-inactive"));

    return Result.Success;
}
```

Or fluently:

```csharp
public Task<Result> EnsureActiveAsync(string id) => Result
    .GoAsync(() => _{dep}Adapter.GetAsync(id))
    .ThenAs(entity => entity.IsInactive
        ? Result.BusinessError("{Name} is inactive.", e => e.WithErrorCode("{name}-inactive"))
        : Result.Success);
```

**`Result.BusinessError` vs `Result.ValidationError`:** use `BusinessError` for domain/state violations that the user understands as a process rule ("entity is no longer available"), and `ValidationError` for constraint violations tied to a specific input field ("Product was not found").

---

## Step 4 — `LText` for Reusable Property Names

When the parameter name (`nameof(id)`) doesn't match the user-facing field label, declare a static `LText` field for the friendly name:

```csharp
public class {Name}Policy(I{Dep}Adapter {dep}Adapter)
{
    private static readonly LText _{param}Text = new("{FriendlyFieldName}");
    private readonly I{Dep}Adapter _{dep}Adapter = {dep}Adapter.ThrowIfNull();

    public Task<Result<Contracts.{Name}>> EnsureExistsAsync(string id) => Result
        .GoAsync(() => _{dep}Adapter.GetAsync(id))
        .OnFailure(r => r.IsNotFoundError
            ? Result.ValidationError(MessageItem.CreateErrorMessage(_{param}Text, "{Name} was not found."))
            : r);
}
```

`LText` is a localizable text wrapper. Using a static field avoids repeated allocation and keeps the property label consistent across all methods in the class.

---

## Step 5 — Multi-Method Policy

Group multiple related guards in one class when they share the same dependency:

```csharp
namespace {Solution}.Application.Policies;

public class {Name}Policy(I{Dep}Adapter {dep}Adapter)
{
    private static readonly LText _{param}Text = new("{FriendlyFieldName}");
    private readonly I{Dep}Adapter _{dep}Adapter = {dep}Adapter.ThrowIfNull();

    /// <summary>Ensures the {Name} exists; returns the loaded entity for reuse by the caller.</summary>
    public Task<Result<Contracts.{Name}>> EnsureExistsAsync(string id) => Result
        .GoAsync(() => _{dep}Adapter.GetAsync(id))
        .OnFailure(r => r.IsNotFoundError
            ? Result.ValidationError(MessageItem.CreateErrorMessage(_{param}Text, "{Name} was not found."))
            : r);

    /// <summary>Ensures the {Name} is in active state.</summary>
    public async Task<Result> EnsureActiveAsync(string id)
    {
        var r = await _{dep}Adapter.GetAsync(id).ConfigureAwait(false);
        if (r.IsFailure)
            return r.AsResult();

        return r.Value.IsInactive
            ? Result.BusinessError("{Name} is inactive.", e => e.WithErrorCode("{name}-inactive"))
            : Result.Success;
    }
}
```

---

## Step 6 — Using the Policy in a Service

Policies are **not DI-registered** — instantiate them at the call site. The constructor arguments come from the service's own injected fields.

### Simple pre-flight check

```csharp
// In a service method — _{dep}Adapter is already injected:
var pr = await new {Name}Policy(_{dep}Adapter).EnsureExistsAsync(item.{Id}!)
    .ConfigureAwait(false);
if (pr.IsFailure)
    return pr.AsResult();

// pr.Value is the loaded entity — use it without a second fetch
```

### Combined validation + policy in a `Result<T>` pipeline

When validation and a policy check are both needed before the transaction, compose them:

```csharp
var pr = await Result.GoAsync(() => {Request}Validator.Default.ValidateWithResultAsync(request))
    .ThenAsAsync(r => new {Name}Policy(_{dep}Adapter).EnsureExistsAsync(r.{Id}!))
    .ConfigureAwait(false);

if (pr.IsFailure)
    return pr.AsResult();

// pr.Value is the entity returned by EnsureExistsAsync
return await OrchestrateUpdateAsync(id, entity => entity.{Action}(pr.Value));
```

**`ThenAsAsync` vs `ThenAsync`:** the delegate changes the result type (`Result<ValidatedRequest>` → `Result<Contracts.{Name}>`), so the `As` variant is required.

### Chaining two policies (both must pass)

```csharp
var pr = await Result.GoAsync(() => new {Name1}Policy(_{dep1}Adapter).EnsureExistsAsync(item.{Id1}!))
    .ThenAsAsync(_ => new {Name2}Policy(_{dep2}Adapter).EnsureExistsAsync(item.{Id2}!))
    .ConfigureAwait(false);

if (pr.IsFailure)
    return pr.AsResult();
```

---

## Phase 2 — Validate and Test

1. `dotnet build` — no errors or warnings.
2. Verify the policy lives in `Application/Policies/` and is **not** added to DI.
3. Verify call sites use `new {Name}Policy(_{dep}).MethodAsync(...)` — never inject via constructor.
4. Verify `NotFoundError` is translated to `ValidationError` — not allowed to propagate as a 404.
5. **Offer to write a unit test** — policies are small, pure, and ideal for isolated testing:

```csharp
[Test]
public async Task EnsureExistsAsync_NotFound_ReturnsValidationError()
{
    var mockAdapter = new MockHttpClientFactory(); // or Moq/NSubstitute mock
    // configure mock to return NotFoundError

    var policy = new {Name}Policy(mockAdapter);
    var result = await policy.EnsureExistsAsync("missing-id").ConfigureAwait(false);

    result.IsFailure.Should().BeTrue();
    result.IsValidationError.Should().BeTrue();
}
```

---

## Guardrails

- **Never register a policy in DI** — instantiate at call site only. If you find yourself registering `I{Name}Policy`, stop and reconsider.
- **Never let `NotFoundException` propagate** from an EnsureExists method — always translate it to `ValidationError` so the HTTP layer returns 422, not 404.
- **`Result.BusinessError` for process/state violations; `Result.ValidationError` for field-constraint violations** — do not use `BusinessError` to signal a missing entity.
- **Return `Result<T>` (entity)** when the caller needs the loaded value; return `Result` (pass/fail) only when the entity is not needed downstream — this avoids a duplicate fetch in the service.
- **Always `.ConfigureAwait(false)` on every `await`** — policy methods run in service context where thread-pool continuations matter.
- **Do not add mutations** to a policy — policies are read-only guard checks. Mutations belong in the service's `TransactionAsync` block.
- **Single responsibility** — one policy class per external domain or per distinct set of related guards sharing the same dependency. Do not aggregate unrelated guards across multiple adapters into one class.
