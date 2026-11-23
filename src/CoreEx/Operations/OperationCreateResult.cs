namespace CoreEx.Operations;

/// <summary>
/// Represents the result of an <see cref="IOperationStore.CreateAsync"/>.
/// </summary>
/// <param name="Created">Indicates whether a new operation was created; otherwise, an operation with the same identity already exists.</param>
/// <param name="State">The created or existing <see cref="OperationState"/>.</param>
public sealed record OperationCreateResult(bool Created, OperationState State);
