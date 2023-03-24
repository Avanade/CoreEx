# CoreEx.Json

The `CoreEx.Json` namespace provides additional [JSON](https://en.wikipedia.org/wiki/JSON)-related capabilities.

<br/>

## Motivation

.NET recently added [`System.Text.Json`](https://docs.microsoft.com/en-us/dotnet/api/system.text.json); however, there is still extensive usage of [`Newtonsoft.Json`](https://www.newtonsoft.com/json), and there can be [challenges migrating](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-migrate-from-newtonsoft-how-to) from the latter to the former depending on capabilties required. As such `CoreEx` is largely JSON serializer agnostic and provides implementations for each; whilst also providing additional JSON-related capabilities.

<br/>

## JSON Serializer

To be JSON serializer agnostic, _CoreEX_ introduces [`IJsonSerializer`](./IJsonSerializer.cs) which is used almost exclusively within _CoreEx_ to encapsulate common capabilities; primarily `Serialize`, `Deserialize` and `TryApplyFilter` (enables include/exclude property filtering).

The following implementations are provided.

 - [`CoreEx.Text.Json.JsonSerializer`](../Text/Json/JsonSerializer.cs) - leverages [`System.Text.Json`](https://docs.microsoft.com/en-us/dotnet/api/system.text.json) (see [`CoreEx.Text.Json`](../Text/Json)).
 - [`CoreEx.Newtonsoft.Json.JsonSerializer`](../../CoreEx.Newtonsoft/Json/JsonSerializer.cs) - leverages [`Newtonsoft.Json`](https://www.newtonsoft.com/json) (see [`CoreEx.Newtonsoft.Json`](../../CoreEx.Newtonsoft/Json)).

<br/>

## JSON Merge

Provides a JSON Merge Patch (`application/merge-patch+json`) whereby the contents of a JSON document are merged into an existing object value as per [RFC7396](https://tools.ietf.org/html/rfc7396). This is used to achieve the exclusive _CoreEx_ HTTP `PATCH` functionality within [`CoreEx.WebApis`](../WebApis).

The [`IJsonMergePatch`](./Merge/IJsonMergePatch.cs) interface and [`JsonMergePatch`](./Merge/JsonMergePatch.cs) implementation enable this functionality. The [`JsonMergePatchOptions`](./Merge/JsonMergePatchOptions.cs) specifies the options to support alternate merge approaches; for example [`DictionaryMergeApproach`](./Merge/DictionaryMergeApproach.cs) and [`EntityKeyCollectionMergeApproach`](./Merge/EntityKeyCollectionMergeApproach.cs).

<br/>

## JSON Data

The [`JsonDataReader`](./Data/JsonDataReader.cs) reads JSON (or YAML) data and converts into a corresponding typed collection; this is primarily intended to enable data loading scenarios.


