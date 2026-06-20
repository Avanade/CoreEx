# CoreEx.Text

> Provides `SentenceCase` — a utility for converting identifiers and PascalCase/camelCase strings to human-readable sentence-case text used in error messages, display labels, and OpenAPI descriptions.

## Overview

`CoreEx.Text` is a small, focused namespace with a single class: `SentenceCase`. It converts programming-style identifier strings (e.g. `"firstName"`, `"FirstName"`, `"FIRST_NAME"`) into readable sentence-case output (e.g. `"First Name"`). This is used by CoreEx validators and code-generation tooling to derive display-friendly property labels from property names without requiring explicit `[Display(Name = ...)]` annotations.

## Key capabilities

- 🔤 **Identifier to sentence case**: `SentenceCase.ToSentenceCase(string)` handles PascalCase, camelCase, underscore-separated, and all-caps identifiers, producing a human-readable label.
- ✏️ **Word-list aware**: Common abbreviations and acronyms (e.g. `ETag`) are preserved as-is rather than being split into individual characters.

## Key types

| Type | Description |
|------|-------------|
| **[`SentenceCase`](./SentenceCase.cs)** | Static utility: `ToSentenceCase(string)` converts a programming identifier to a human-readable sentence-case string. |

## Related Namespaces

- **[`CoreEx.Validation`](../../CoreEx.Validation/README.md)** - Validator rule error messages derive display-friendly property names from `SentenceCase.ToSentenceCase` when no explicit label is configured.
- **[`CoreEx`](../README.md)** - Root package; code-generation tooling uses `SentenceCase` when producing OpenAPI descriptions from property names.