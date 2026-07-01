---
mode: agent
description: "Create or modify a CoreEx adapter (anti-corruption layer): Application interface, Infrastructure implementation, typed HTTP client, and unit tests."
---

Your job is to create or modify a CoreEx adapter following the project's established conventions.

Read and follow `.github/skills/coreex-adapter/references/workflow.md` exactly. Do not skip steps or invent patterns not shown there.

Reminder of key conventions:
- Sub-folder per external system is **always required**: `Application/Adapters/{ExternalSystem}/`, `Infrastructure/Adapters/{ExternalSystem}/`, `Infrastructure/Clients/{ExternalSystem}/`
- All methods return `Result` or `Result<T>` — never plain values at the adapter boundary
- `[ScopedService<IXxxAdapter>]` on every implementation
- `response.ToResultAsync(ct)` for all HTTP responses — never `EnsureSuccessStatusCode()`
- `CancellationToken.None` in compensation paths (not the request `ct`)
- HTTP client tests use `WithGenericTester<EntryPoint>` + `Test.ReplaceHttpClientFactory(mcf)` — resolve the client from DI via `ExecutionContext.GetRequiredService<T>()`, not `new T()` directly
- `UnitTestEx.MockHttpClientFactory.Create()` (fully-qualified) to avoid the ambiguity with `UnitTestEx.Mocking.MockHttpClientFactory`; store the request mock as a field and call `.WithAnyBody()` per test before `.Respond`

${input}
