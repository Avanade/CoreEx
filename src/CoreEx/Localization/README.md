# CoreEx.Localization

> Provides `LText` — a lightweight localization-agnostic text container — and `TextProvider` / `ITextProvider` for pluggable resource-key-to-string resolution used throughout CoreEx for user-facing messages.

## Overview

`CoreEx.Localization` decouples user-facing string content from the code that defines it. `LText` is a readonly struct that carries either a raw string, a resource key, or both — along with optional fallback text and format arguments. At the point of display or serialization, the ambient `TextProvider.Current` is called to resolve the final string, allowing the application to plug in any localization source (resource files, databases, translation services) without changing message definitions.

The default `TextProvider` returns `LText.KeyAndOrText` unchanged (the key itself acts as the display string), making the system work out-of-the-box without any localization infrastructure. A custom provider is registered by setting `TextProvider.SetTextProvider(ITextProvider)` at startup.

CoreEx uses `LText` internally for all validation error messages, exception messages, and formatted user-facing text, ensuring that every system-generated message can be localized by the consuming application.

## Key capabilities

- 🌐 **Localization-agnostic text**: `LText` carries a key and/or text value with optional fallback text and format arguments; resolution is deferred to the ambient `TextProvider.Current`.
- 🔌 **Pluggable text provider**: `ITextProvider` is a single-method interface (`GetText`); any implementation can be registered as the application-wide text resolver via `TextProvider.SetTextProvider()`.
- 📝 **Formatted messages**: `LText` supports format argument arrays; `TextProvider` applies `string.Format` after key resolution, enabling `"Field is required: {0}"` style messages with runtime values.
- ✅ **Zero-config default**: `NullTextProvider` returns the raw `KeyAndOrText` value unchanged, so the system works without any localization setup for development and testing.
- 🏷️ **Localization attribute**: `[LocalizationAttribute]` marks a property with a corresponding localizable `LText` definition, enabling runtime (reflection-based) discovery of localizable content.

## Key types

| Type | Description |
|------|-------------|
| **[`LText`](./LText.cs)** | Readonly struct carrying a `KeyAndOrText`, optional `FallbackText`, optional format `Args`, and `WasFallBackTextSetToNull` flag; implicitly converts from `string`. |
| **[`TextProvider`](./TextProvider.cs)** | Static accessor for the ambient `ITextProvider`; call `TextProvider.SetTextProvider(impl)` at startup to register a custom provider; `TextProvider.Current` resolves the active provider. |
| _[`TextProviderBase`](./TextProviderBase.cs)_ | Abstract base for `ITextProvider` implementations; handles fallback logic (key lookup → fallback text → key-as-text). |
| **[`NullTextProvider`](./NullTextProvider.cs)** | Default `ITextProvider` that returns `LText.KeyAndOrText` unchanged — no resource lookup is performed. |
| [`ITextProvider`](./ITextProvider.cs) | Interface with a single `GetText(LText)` method returning the resolved string. |
| [`LocalizationAttribute`](./LocalizationAttribute.cs) | Property attribute marking the presence of a localizable `LText` definition for runtime (reflection-based) discovery. |

## Related Namespaces

- **[`CoreEx`](../README.md)** - Semantic exception types such as `ValidationException`, `BusinessException`, and `NotFoundException` carry `LText` messages that pass through `TextProvider` when rendered.
- **[`CoreEx.Validation`](../Validation/README.md)** - All built-in validation rule error messages are defined as `LText` constants, resolved at runtime via `TextProvider.Current`.
- **[`CoreEx.Entities`](../Entities/README.md)** - `MessageItem.Text` is of type `LText`, enabling field-level validation messages to participate in localization.