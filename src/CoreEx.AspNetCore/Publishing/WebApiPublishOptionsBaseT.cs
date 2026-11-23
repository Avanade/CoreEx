namespace CoreEx.AspNetCore.Publishing;

/// <summary>
/// Provides fluent shared publishing configuration while preserving the concrete options type.
/// </summary>
/// <typeparam name="TSelf">The concrete options type.</typeparam>
public abstract class WebApiPublishOptionsBase<TSelf> : WebApiPublishOptionsBase where TSelf : WebApiPublishOptionsBase<TSelf>
{
    /// <summary>
    /// Sets the destination override.
    /// </summary>
    /// <param name="destination">The destination, or <see langword="null"/> to use the publisher default.</param>
    /// <returns>The current options.</returns>
    /// <remarks>Destination overrides are supported only for untracked publishing; tracked commands use the configured publisher's default routing.</remarks>
    public TSelf WithDestination(string? destination)
    {
        Destination = destination;
        return (TSelf)this;
    }

    /// <summary>
    /// Sets the publisher resolver.
    /// </summary>
    /// <param name="publisherProvider">The resolver for the publisher.</param>
    /// <returns>The current options.</returns>
    public TSelf WithPublisher(Func<IServiceProvider, IEventPublisher> publisherProvider)
    {
        PublisherProvider = publisherProvider.ThrowIfNull();
        return (TSelf)this;
    }

    /// <summary>
    /// Sets a transformation applied to the prepared event before operation metadata is attached.
    /// </summary>
    /// <param name="eventTransform">The event transformation.</param>
    /// <returns>The current options.</returns>
    /// <remarks>For tracked publishing, preserve the declared payload type; that type selects operation registration.</remarks>
    public TSelf WithEvent(Func<EventData, EventData> eventTransform)
    {
        SetEventTransform(eventTransform.ThrowIfNull());
        return (TSelf)this;
    }

    /// <summary>
    /// Sets the optional operation timing configuration.
    /// </summary>
    /// <param name="operationOptions">The operation timing configuration, or <see langword="null"/> to clear it.</param>
    /// <returns>The current options.</returns>
    public TSelf WithOperationOptions(WebApiPublishOperationOptions? operationOptions)
    {
        Operation = operationOptions;
        return (TSelf)this;
    }
}
