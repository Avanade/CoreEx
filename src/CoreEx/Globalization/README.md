# CoreEx.Globalization

> Provides `TextInfoCasing` utility and `GlobalizationExtensions` for culture-aware string casing operations used in entity cleaning and reference data normalization.

## Overview

`CoreEx.Globalization` is a small utility namespace that wraps `System.Globalization.TextInfo` to provide deterministic, culture-aware `ToUpperCase`, `ToLowerCase`, and `ToTitleCase` operations used by `Cleaner` (via `StringCase`) and reference data code normalization.

By centralizing these calls here, CoreEx ensures that casing operations consistently use the invariant culture (or a configured `CultureInfo`) rather than the ambient thread culture, avoiding locale-sensitive bugs in entity normalization.

## Key capabilities

- 🔤 **Culture-aware casing**: `TextInfoCasing` wraps `TextInfo.ToUpper`, `ToLower`, and `ToTitleCase` to apply casing with a specified (or invariant) culture.
- 🧩 **Extension methods**: `GlobalizationExtensions` provides `ToUpperInvariant`, `ToLowerInvariant`, and `ToTitleCaseInvariant` string extension methods as convenience wrappers.

## Key types

| Type | Description |
|------|-------------|
| **[`TextInfoCasing`](./TextInfoCasing.cs)** | Utility providing `ToUpper(string)`, `ToLower(string)`, and `ToTitleCase(string)` using a configured `CultureInfo` (default: `CultureInfo.InvariantCulture`). |
| **[`GlobalizationExtensions`](./GlobalizationExtensions.cs)** | String extension methods: `ToUpperInvariant()`, `ToLowerInvariant()`, `ToTitleCaseInvariant()` delegating to `TextInfoCasing`. |

## Related Namespaces

- **[`CoreEx.Entities`](../Entities/README.md)** - `Cleaner` uses `GlobalizationExtensions` for the `StringCase.Upper`, `Lower`, and `Title` transformations applied to entity property values during cleaning.