namespace CoreEx.Events.Subscribing;

/// <summary>
/// The <see cref="ErrorHandler"/> arguments.
/// </summary>
public sealed class ErrorHandlerArgs
{
    /// <summary>
    /// Gets the corresponding <see cref="EventSubscriberArgs"/>.
    /// </summary>
    public required EventSubscriberArgs SubscriberArgs { get; init; }   
    
    /// <summary>
    /// Gets the owning <see cref="EventSubscriberBase"/>.
    /// </summary>
    public EventSubscriberBase Subscriber => SubscriberArgs.Owner ?? throw new InvalidOperationException($"The {nameof(SubscriberArgs)}.{nameof(SubscriberArgs.Owner)} has not been set.");

    /// <summary>
    /// Gets the <see cref="ILogger"/>.
    /// </summary>
    public ILogger Logger => Subscriber.Logger;

    /// <summary>
    /// Gets the source <see cref="Type"/> of the subscriber that is handing the error.
    /// </summary>
    public required Type SourceType { get; init; }

    /// <summary>
    /// Gets the <see cref="Exception"/> that must be handled.
    /// </summary>
    public required Exception Exception { get; init; }

    /// <summary>
    /// Gets the optional <see cref="ErrorHandling"/> override (bypassing <see cref="ErrorHandler"/> configuration).
    /// </summary>
    public ErrorHandling? ErrorHandlingOverride { get; init; }
}