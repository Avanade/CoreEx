namespace CoreEx.IO.Json;

/// <summary>Provides internal dispatch for declared nested occurrences and metadata.</summary>
internal interface IJsonUnitMapping
{
    /// <summary>Maps an object and its declared descendants.</summary>
    /// <param name="element">The JSON object to map.</param>
    /// <param name="context">The pass-local mapping and diagnostic context.</param>
    /// <param name="propertyPath">The current model path.</param>
    /// <param name="sourcePath">The current JSON Pointer.</param>
    /// <param name="structuralMember">A structural member excluded from ordinary unknown-member checks.</param>
    /// <param name="cancellationToken">The processing cancellation token.</param>
    /// <returns>The mapped wire object.</returns>
    Task<object> MapAsync(JsonElement element, JsonMappingContext context, string propertyPath, string sourcePath, string? structuralMember, CancellationToken cancellationToken);

    /// <summary>Resolves a validator's model path to a declared source path.</summary>
    /// <param name="propertyPath">The validator's model path.</param>
    /// <returns>The corresponding JSON Pointer.</returns>
    string ResolveSourcePath(string propertyPath);

    /// <summary>Restores CLR property segments when a validator emits its serializer-oriented names.</summary>
    /// <param name="propertyPath">The validator's model or serializer path.</param>
    /// <returns>The path using CLR property names.</returns>
    string ResolvePropertyPath(string propertyPath);
}
