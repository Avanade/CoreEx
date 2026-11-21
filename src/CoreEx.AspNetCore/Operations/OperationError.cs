namespace CoreEx.AspNetCore.Operations;

/// <summary>
/// Represents a safe public operation error.
/// </summary>
/// <param name="Code">The machine-readable error code.</param>
/// <param name="Title">The user-safe error title.</param>
public sealed record OperationError(string Code, string Title);
