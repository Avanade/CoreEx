namespace CoreEx.Operations;

/// <summary>
/// Provides the <see cref="OperationStoreBase"/> abandonment and retention options.
/// </summary>
public sealed class OperationStoreOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OperationStoreOptions"/> class using ambient configuration where available.
    /// </summary>
    public OperationStoreOptions() : this(null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="OperationStoreOptions"/> class.
    /// </summary>
    /// <param name="configuration">The optional configuration; when omitted, ambient configuration is used.</param>
    public OperationStoreOptions(IConfiguration? configuration)
    {
        InactivityTimeout = Internal.GetConfigurationValue("CoreEx:Operations:InactivityTimeout", TimeSpan.FromHours(24), configuration);
        TerminalRetention = Internal.GetConfigurationValue("CoreEx:Operations:TerminalRetention", TimeSpan.FromHours(24), configuration);
    }

    /// <summary>
    /// Gets or sets how long an unfinished operation may go without activity (create, claim expiry, renewal or release) before cleanup expires it as abandoned; defaults to 24 hours.
    /// </summary>
    /// <remarks>This is a delivery and recovery inactivity window, not a work-duration or scheduling deadline. It must exceed the broker's complete redelivery window (maximum delivery count multiplied by lock duration plus any retry back-off), otherwise work that is still legitimately queued is expired. The value is applied when the operation is written, so changing it affects only subsequent writes.</remarks>
    public TimeSpan InactivityTimeout
    {
        get;
        set => field = value > TimeSpan.Zero ? value : throw new ArgumentOutOfRangeException(nameof(InactivityTimeout), "The timeout must be greater than zero.");
    }

    /// <summary>
    /// Gets or sets how long a finished operation is retained before it may be purged; defaults to 24 hours.
    /// </summary>
    public TimeSpan TerminalRetention
    {
        get;
        set => field = value > TimeSpan.Zero ? value : throw new ArgumentOutOfRangeException(nameof(TerminalRetention), "The retention must be greater than zero.");
    }
}
