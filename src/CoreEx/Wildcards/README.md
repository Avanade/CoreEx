# CoreEx.Wildcards

> Provides `Wildcard` — a configurable wildcard parser that validates and classifies user-supplied wildcard text (e.g. `*` and `?`) and produces a `WildcardResult` for downstream `LIKE`-style filtering in queries.

## Overview

`CoreEx.Wildcards` standardizes how wildcard text entered by users is validated and represented before being translated to a database `LIKE` clause or an in-memory predicate. Instead of scattering ad-hoc `%` / `*` handling across repositories, consuming code creates a `Wildcard` configuration instance specifying which wildcard characters and space treatments are supported, then calls `Parse(text)` to get a validated `WildcardResult`.

`WildcardResult` encodes the parsed outcome: whether the text is an exact match, a starts-with, ends-with, contains, or a full multi-wildcard pattern — each as a `WildcardSelection` flags enum value. ORM integrations in `CoreEx.Data` and `CoreEx.EntityFrameworkCore` consume `WildcardResult` to produce the correct `LIKE` expression without duplicating this logic.

## Key capabilities

- 🔍 **Configurable wildcard characters**: `Wildcard` supports the standard `*` (multi-character) and `?` (single-character) wildcards; which characters are active is controlled by `WildcardSelection` flags at construction.
- 🧩 **Space treatment**: `WildcardSpaceTreatment` controls whether spaces in wildcard text are converted to `*`, treated as `?`, or left unchanged, accommodating different user-facing search conventions.
- ✅ **Validation and classification**: `Wildcard.Parse(text)` validates the input against the configured `WildcardSelection` and returns a `WildcardResult` carrying the classification (equal, startsWith, endsWith, contains, multiWildcard) and the sanitized text.
- 📐 **Pre-defined instances**: `Wildcard.None`, `Wildcard.MultiBasic`, `Wildcard.MultiAll`, and `Wildcard.All` provide ready-made configurations for common scenarios, reducing boilerplate.
- 🔗 **ORM integration bridge**: `WildcardResult` is consumed by `CoreEx.Data` and `CoreEx.EntityFrameworkCore` query-building utilities to produce the correct `LIKE` / `EF.Functions.Like` expression.

## Key types

| Type | Description |
|------|-------------|
| **[`Wildcard`](./Wildcard.cs)** | Configured wildcard parser: `Parse(string)` validates input and returns a `WildcardResult`; static instances `None`, `MultiBasic`, `MultiAll`, `All`. |
| **[`WildcardResult`](./WildcardResult.cs)** | Parsed result: `Selection` (`WildcardSelection` flags), sanitized `Text`, `IsValid`, `HasWildcard`, and convenience bool properties (`IsEqual`, `IsStartsWith`, etc.). |
| **[`WildcardSelection`](./WildcardSelection.cs)** | Flags enum describing the wildcard type: `None`, `Equal`, `Single` (`?`), `MultiBasic` (`*`), `MultiAll`, `BothEnds`, `Contains`. |
| **[`WildcardSpaceTreatment`](./WildcardSpaceTreatment.cs)** | Enum controlling space handling: `None` (leave as-is), `MultiWildcard` (convert to `*`), `SingleWildcard` (convert to `?`). |

## Related Namespaces

- **[`CoreEx`](../README.md)** - Root package; `ReferenceDataOrchestrator` uses `Wildcard.MultiAll` for text-based reference data search.
- **[`CoreEx.Data`](../Data/README.md)** - `QueryArgs` includes optional wildcard configuration; `CoreEx.Data` query builders consume `WildcardResult` to produce `LIKE` expressions.
- **[`CoreEx.EntityFrameworkCore`](../../CoreEx.EntityFrameworkCore/README.md)** - EF Core query extensions translate `WildcardResult` into `EF.Functions.Like(...)` predicates.