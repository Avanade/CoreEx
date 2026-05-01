# JsonDataReader vs JsonNodeDataReader

## Overview

`JsonNodeDataReader` is the **mutable** alternative to `JsonDataReader`, using `JsonNode` instead of `JsonElement`.

## Key Differences

| Feature | JsonDataReader | JsonNodeDataReader |
|---------|----------------|-------------------|
| **Underlying Type** | `JsonElement` (struct) | `JsonNode` (class hierarchy) |
| **Mutability** | Immutable/Read-only | Mutable - can modify returned nodes |
| **Memory Model** | Backed by `JsonDocument` | Standalone object graph |
| **Disposal** | Requires `IDisposable` | No disposal needed |
| **Root Property** | `RootElement` (JsonElement) | `RootNode` (JsonNode) |
| **Type Checking** | `ValueKind` enum | Type patterns (`is JsonObject`) |
| **Performance** | Lower memory overhead | Higher allocation cost |
| **Thread Safety** | Safe (immutable) | Unsafe (mutable) |

## API Comparison

### Creating Instances

```csharp
// JsonDataReader
var reader = JsonDataReader.ParseJson(jsonString);
using (reader) // Must dispose
{
    var root = reader.RootElement;
}

// JsonNodeDataReader
var nodeReader = JsonNodeDataReader.ParseJson(jsonString);
// No disposal needed
var root = nodeReader.RootNode;
```

### Getting Data

```csharp
// JsonDataReader
if (reader.TryGetPath("path.to.data", out JsonElement element))
{
    // element is read-only
}

// JsonNodeDataReader
if (nodeReader.TryGetPath("path.to.data", out JsonNode? node))
{
    // node can be modified
    if (node is JsonObject obj)
    {
        obj["newProperty"] = "new value"; // Mutation!
    }
}
```

### Creating Data with Parameters

```csharp
// JsonDataReader
if (reader.TryCreateData("data", out JsonElement? result, properties, parameters))
{
    // result is immutable
}

// JsonNodeDataReader
if (nodeReader.TryCreateData("data", out JsonNode? result, properties, parameters))
{
    // result can be modified after creation
    if (result is JsonArray arr)
    {
        arr.Add(JsonValue.Create("new item")); // Can add items!
    }
}
```

## When to Use Each

### Use JsonDataReader When:
- ✅ You need maximum performance and minimal allocations
- ✅ Read-only access is sufficient
- ✅ Working with large JSON documents
- ✅ Thread-safety is important

### Use JsonNodeDataReader When:
- ✅ You need to mutate the JSON after loading
- ✅ Building or modifying JSON structures dynamically
- ✅ Working with smaller datasets where allocation overhead is acceptable
- ✅ You prefer working with object-oriented APIs

## Migration Path

To migrate from `JsonDataReader` to `JsonNodeDataReader`:

1. Change the type:
   ```csharp
   // Before
   var reader = JsonDataReader.ParseJson(json);

   // After
   var reader = JsonNodeDataReader.ParseJson(json);
   ```

2. Update property access:
   ```csharp
   // Before
   JsonElement root = reader.RootElement;

   // After
   JsonNode root = reader.RootNode;
   ```

3. Remove disposal (if using `using`):
   ```csharp
   // Before
   using var reader = JsonDataReader.ParseJson(json);

   // After (no disposal needed)
   var reader = JsonNodeDataReader.ParseJson(json);
   ```

4. Update type checking:
   ```csharp
   // Before
   if (element.ValueKind == JsonValueKind.Object)

   // After
   if (node is JsonObject obj)
   ```

5. Update parameter functions:
   ```csharp
   // Before
   var params = new Dictionary<string, Func<JsonDataReaderArgs, object?>>();

   // After
   var params = new Dictionary<string, Func<JsonNodeDataReaderArgs, object?>>();
   ```

## Internal Implementation Differences

### JsonDataReaderArgs vs JsonNodeDataReaderArgs

```csharp
// JsonDataReaderArgs
public class JsonDataReaderArgs
{
    public JsonElement Root { get; init; }
    public JsonProperty Current { get; internal set; } // Struct with Name + Value
}

// JsonNodeDataReaderArgs
public class JsonNodeDataReaderArgs
{
    public JsonNode? Root { get; init; }
    public string? CurrentPropertyName { get; internal set; } // Separate name
    public JsonNode? CurrentValue { get; internal set; }      // and value
}
```

### Copying Logic

- **JsonDataReader**: Uses `Utf8JsonWriter` to write, then re-parses to `JsonElement`
- **JsonNodeDataReader**: Creates new `JsonObject`/`JsonArray`/`JsonValue` instances directly

## Performance Considerations

```csharp
// JsonDataReader - minimal allocations
var reader = JsonDataReader.ParseJson(largeJson);
var element = reader.RootElement; // Zero-copy access to document

// JsonNodeDataReader - object allocations
var nodeReader = JsonNodeDataReader.ParseJson(largeJson);
var node = nodeReader.RootNode; // Allocates object graph
```

## Deprecation Timeline

- **Phase 1** (Current): Both APIs available side-by-side
- **Phase 2** (Future): Mark `JsonDataReader` as `[Obsolete]` with migration guidance
- **Phase 3** (Major version): Remove `JsonDataReader` if JsonNodeDataReader proves superior

## Examples

See unit tests in:
- `tests/CoreEx.Data.Test.Unit/Json/JsonDataReaderTests.cs` (existing)
- `tests/CoreEx.Data.Test.Unit/Json/JsonNodeDataReaderTests.cs` (to be created)
