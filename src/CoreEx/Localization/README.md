# CoreEx.Localization

The `CoreEx.Localization` namespace provides additional globalization. capabilities.

<br/>

## Motivation

To enable extended additional localization capabilities.

<br/>

## Text localization

To simplify the localization of strings a localized text class, [`LText`](./LText.cs), has been introduced.

The `LText` supports a constructor that takes a `keyAndOrText` being the key and/or text used to lookup the localized value, and an optional `fallbackText` to be used where the lookup fails. Where no text is found, then the originating `keyAndOrText` will be used.

Additionally, the `LText` supports an implicit operator to and from a `string`, which enables the casting thereof providing a natural development experience. 

The casting to a `string` is the action that performs the lookup. This invokes the static [`TextProvider.Current`](./TextProvider.cs) property, which is an instance of the [`ITextProvider`](./ITextProvider.cs) interface.

<br/>

## Text provider

To support the `LText` lookup functionality an [`ITextProvider`](./ITextProvider.cs) implementation is required to enable. It is the responsibility of the `GetText` method to perform using the following logic, use: a) the corresponding text where found, b) the fallback text, and finally c) the key itself. 

An implementation is provided within [`CoreEx.Validation`](../../CoreEx.Validation), being [`ValidationTextProvider`](../../CoreEx.Validation/ValidationTextProvider.cs) that uses embedded resources for the strings.


