namespace CoreEx.Idempotency;

/// <summary>
/// Describes an atomic processing or replay decision.
/// </summary>
public enum IdempotencyOutcome
{
    /// <summary>An owned attempt was created.</summary>
    Acquired = 0,

    /// <summary>The original attempt is active.</summary>
    InProgress = 1,

    /// <summary>A terminal response can be replayed.</summary>
    Completed = 2,

    /// <summary>The terminal response was too large to retain.</summary>
    ResponseUnavailable = 3,

    /// <summary>The key was used with a different fingerprint.</summary>
    DifferentRequest = 4,

    /// <summary>The original outcome is unknown and must not automatically execute again.</summary>
    Indeterminate = 5
}
