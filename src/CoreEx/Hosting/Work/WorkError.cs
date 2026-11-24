namespace CoreEx.Hosting.Work;

/// <summary>
/// Represents a safe, serializable work failure.
/// </summary>
/// <param name="Code">The machine-readable error code.</param>
/// <param name="Message">The user-safe error message.</param>
/// <param name="IsTransient">Whether retrying may succeed.</param>
public sealed record WorkError(string Code, string Message, bool IsTransient = false);
