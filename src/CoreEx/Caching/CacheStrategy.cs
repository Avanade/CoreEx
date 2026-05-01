namespace CoreEx.Caching;

/// <summary>
/// Represents the underlying cache strategy.
/// </summary>
[Flags]
public enum CacheStrategy
{
    /// <summary>
    /// Caching is applied locally; generally in-memory.
    /// </summary>
    Local = 1,

    /// <summary>
    /// Caching is applied in a distributed manner.
    /// </summary>
    Distributed = 2,

    /// <summary>
    /// Indicates caching is applied both <see cref="Local"/> (L1) and <see cref="Distributed"/> (L2).
    /// </summary>
    Hybrid = Local | Distributed,
}