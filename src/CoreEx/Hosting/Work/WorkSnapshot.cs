namespace CoreEx.Hosting.Work;

/// <summary>
/// Represents a work-state snapshot and the time observed by its provider.
/// </summary>
/// <param name="State">The state where found; otherwise, <see langword="null"/>.</param>
/// <param name="Now">The provider's current UTC time at the read.</param>
public readonly record struct WorkSnapshot(WorkState? State, DateTimeOffset Now);
