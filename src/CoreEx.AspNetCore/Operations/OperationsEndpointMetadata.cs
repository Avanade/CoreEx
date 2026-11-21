namespace CoreEx.AspNetCore.Operations;

/// <summary>
/// Identifies a generic asynchronous-operation endpoint for OpenAPI generation.
/// </summary>
/// <param name="Kind">The operation endpoint kind.</param>
public sealed record OperationsEndpointMetadata(OperationsEndpointKind Kind);
