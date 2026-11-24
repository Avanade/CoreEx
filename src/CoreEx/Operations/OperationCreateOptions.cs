namespace CoreEx.Operations;

/// <summary>
/// Provides optional lifetime overrides captured when an operation is first created.
/// </summary>
public sealed class OperationCreateOptions
{
    /// <summary>
    /// Gets or sets the inactivity timeout; <see langword="null"/> uses the store default.
    /// </summary>
    /// <remarks>This is a delivery and recovery window, not a processing deadline.</remarks>
    public TimeSpan? InactivityTimeout { get; set; }

    /// <summary>
    /// Gets or sets terminal retention; <see langword="null"/> uses the store default.
    /// </summary>
    public TimeSpan? TerminalRetention { get; set; }
}
