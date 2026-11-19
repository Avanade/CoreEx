namespace CoreEx.Coordination;

/// <summary>
/// Defines bounded ownership and record retention for coordination providers.
/// </summary>
public sealed record CoordinationOptions
{
    /// <summary>
    /// Gets the ownership duration.
    /// </summary>
    public TimeSpan LeaseDuration { get; init; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Gets the automatic renewal interval.
    /// </summary>
    public TimeSpan RenewalInterval { get; init; } = TimeSpan.FromSeconds(20);

    /// <summary>
    /// Gets the maximum duration of a backend operation.
    /// </summary>
    public TimeSpan OperationTimeout { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets the terminal record retention, also retained beyond processing expiry.
    /// </summary>
    public TimeSpan Retention { get; init; } = TimeSpan.FromHours(8);

    /// <summary>
    /// Validates the duration and renewal safety margins.
    /// </summary>
    public void Validate()
    {
        if (LeaseDuration.TotalMilliseconds < 1 || RenewalInterval.TotalMilliseconds < 1 || OperationTimeout.TotalMilliseconds < 1 || Retention.TotalMilliseconds < 1
            || RenewalInterval >= LeaseDuration || OperationTimeout >= LeaseDuration - RenewalInterval - TimeSpan.FromMilliseconds(10)
            || LeaseDuration.TotalMilliseconds > int.MaxValue || Retention.TotalMilliseconds > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(CoordinationOptions), "Durations must be positive, bounded, and allow renewal plus its timeout before lease expiry.");
    }
}
