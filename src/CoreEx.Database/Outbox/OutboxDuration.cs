namespace CoreEx.Database.Outbox;

/// <summary>
/// Provides <see cref="TimeSpan"/> to whole-second conversion capabilities for outbox lease/backoff durations.
/// </summary>
public static class OutboxDuration
{
    /// <summary>
    /// Converts a duration time-span into a rounded number of seconds where the minimum allowed is one second.
    /// </summary>
    /// <param name="duration">The <see cref="TimeSpan"/> duration.</param>
    /// <returns>The number of seconds; a minimum of one.</returns>
    public static int ToSeconds(TimeSpan duration) => Math.Max((int)Math.Round(duration.TotalSeconds, MidpointRounding.AwayFromZero), 1);

    /// <summary>
    /// Converts a lease duration time-span into a rounded number of seconds, with an additional buffer of 10% (minimum of one second) to minimize the risk of the batch being cancelled due to
    /// exceeding the lease duration before the relay operation has had a chance to complete.
    /// </summary>
    /// <param name="leaseDuration">The lease <see cref="TimeSpan"/> duration.</param>
    /// <returns>The buffered number of seconds.</returns>
    public static int ToLeaseSecondsWithBuffer(TimeSpan leaseDuration)
    {
        var seconds = ToSeconds(leaseDuration);
        return seconds + Math.Max(1, (int)Math.Round(seconds * 0.1, MidpointRounding.AwayFromZero));
    }
}
