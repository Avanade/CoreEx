---
mode: agent
description: "Create or modify a CoreEx adapter (anti-corruption layer): Application interface, Infrastructure implementation, typed HTTP client, and unit tests."
---

Your job is to create or modify a CoreEx adapter following the project's established conventions.

Read and follow `.github/skills/coreex-adapter/references/workflow.md` exactly. Do not skip steps or invent patterns not shown there.

Reminder of key conventions:
- Sub-folder per external domain is **always required**: `Application/Adapters/{ExternalDomain}/`, `Infrastructure/Adapters/{ExternalDomain}/`, `Infrastructure/Clients/{ExternalDomain}/`
- All methods return `Result` or `Result<T>` — never plain values at the adapter boundary
- `[ScopedService<IXxxAdapter>]` on every implementation
- `response.ToResultAsync()` for all HTTP responses — never `EnsureSuccessStatusCode()`
- `CancellationToken.None` in compensation paths (not the request `ct`)
- `UnitTestEx.MockHttpClientFactory.Create()` (fully-qualified) in tests to avoid the ambiguity with `UnitTestEx.Mocking.MockHttpClientFactory`; always call `.WithAnyBody()` on request mocks before `.Respond`

${input}
