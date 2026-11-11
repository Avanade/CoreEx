namespace CoreEx.IO.Json;

/// <summary>Configures serializer contracts and resolves declared model/source paths.</summary>
internal interface IJsonUnitMapping
{
    /// <summary>Deserializes a selected object using the standard asynchronous serializer.</summary>
    Task<object?> DeserializeAsync(JsonDocumentCursor cursor, JsonMappingContext context, string sourcePath, CancellationToken cancellationToken);

    /// <summary>Deserializes a previously parsed JSONL object using the standard serializer.</summary>
    Task<object?> MapAsync(JsonElement element, JsonMappingContext context, string propertyPath, string sourcePath, string? structuralMember, CancellationToken cancellationToken);

    /// <summary>Registers this type and its nested serializer contracts.</summary>
    void RegisterContracts(Dictionary<Type, IJsonUnitMapping> contracts, HashSet<IJsonUnitMapping> visited);

    /// <summary>Applies declared source names and scalar converters to a serializer contract.</summary>
    void Configure(JsonTypeInfo typeInfo, string? structuralMember);

    /// <summary>Gets whether another mapping declares the same property names and types.</summary>
    bool HasSameShape(IJsonUnitMapping other);

    /// <summary>Resolves a serializer exception path without exposing undeclared input member names.</summary>
    (string Property, string Source, bool Unknown) ResolveErrorPath(string path);

    /// <summary>Resolves a validator's model path to a declared source path.</summary>
    string ResolveSourcePath(string propertyPath);

    /// <summary>Restores CLR property segments when a validator emits its serializer-oriented names.</summary>
    string ResolvePropertyPath(string propertyPath);
}
