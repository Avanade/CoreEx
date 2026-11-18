namespace CoreEx.IO.Json;

/// <summary>Defines the JSON value selected as the business-unit source.</summary>
public enum JsonDocumentKind
{
    /// <summary>Selects each object in an array.</summary>
    Array,

    /// <summary>Selects the root object as the single business unit.</summary>
    Object
}
