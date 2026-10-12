namespace CoreEx.Azure.Messaging.ServiceBus;

/// <summary>
/// Provides optional destination-specific Service Bus session settings.
/// </summary>
public sealed class ServiceBusDestinationSettings
{
    /// <summary>
    /// Gets or sets the destination-specific session strategy, or <see langword="null"/> to use the publisher default.
    /// </summary>
    public ServiceBusSessionStrategy? SessionIdStrategy { get; set; }

    /// <summary>
    /// Gets or sets the destination-specific session bucket count when <see cref="SessionIdStrategy"/> is
    /// <see cref="ServiceBusSessionStrategy.UsePartitionKeyConvertedToAnId"/>, or <see langword="null"/> to use the publisher default.
    /// </summary>
    public int? SessionIdPartitionSize { get; set; }
}
