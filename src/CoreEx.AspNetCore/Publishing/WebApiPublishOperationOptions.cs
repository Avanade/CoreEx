namespace CoreEx.AspNetCore.Publishing;

/// <summary>
/// Configures tracked acceptance for one publish operation.
/// </summary>
public sealed record WebApiPublishOperationOptions
{
    /// <summary>
    /// Gets or initializes the registered operation name; may be omitted where the published message type has exactly one registration.
    /// </summary>
    public string? OperationName { get; init; }

    /// <summary>
    /// Gets or initializes the submission identity. Where absent, a typed key selector may supply it; otherwise each acceptance creates fresh work.
    /// </summary>
    /// <remarks>This is not necessarily a business entity identifier. The framework does not read the HTTP idempotency key automatically.</remarks>
    public CompositeKey? Key { get; init; }

    /// <summary>
    /// Gets or initializes the operation's inactivity timeout; <see langword="null"/> uses the store default.
    /// </summary>
    /// <remarks>This is a delivery and recovery window, not a processing deadline.</remarks>
    public TimeSpan? InactivityTimeout { get; init; }

    /// <summary>
    /// Gets or initializes the terminal retention period; <see langword="null"/> uses the store default.
    /// </summary>
    public TimeSpan? TerminalRetention { get; init; }
}
