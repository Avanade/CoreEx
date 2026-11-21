namespace CoreEx.AspNetCore.Publishing;

/// <summary>
/// Configures message creation, publisher resolution and optional operation tracking for Web API publishing.
/// </summary>
public class WebApiPublishOptions
{
    /// <summary>
    /// Gets or sets the <see cref="CoreEx.Events.MessageType"/> used to create the default message. Defaults to <see cref="CoreEx.Events.MessageType.Command"/>.
    /// </summary>
    /// <remarks>Only <see cref="CoreEx.Events.MessageType.Command"/> and <see cref="CoreEx.Events.MessageType.Event"/> are supported. Applied by <see cref="CreateDefaultMessage{T}(T)"/> only.</remarks>
    public CoreEx.Events.MessageType MessageType { get; set; } = CoreEx.Events.MessageType.Command;

    /// <summary>
    /// Gets or sets the action used to create the default message; see <see cref="EventData.Action"/>.
    /// </summary>
    /// <remarks>Where not specified, a <see cref="CoreEx.Events.MessageType.Command"/> defaults to <see cref="EventAction.Publish"/>, and an <see cref="CoreEx.Events.MessageType.Event"/> defaults to <see cref="EventAction.Published"/>. Applied by <see cref="CreateDefaultMessage{T}(T)"/> only.</remarks>
    public string? Action { get; set; }

    /// <summary>
    /// Gets or sets the target domain name used to create the default message; see <see cref="EventData.DomainName"/>.
    /// </summary>
    /// <remarks>For a <see cref="CoreEx.Events.MessageType.Command"/> this is the <i>target</i> domain; where not specified the command is self-addressed (current host domain). For an <see cref="CoreEx.Events.MessageType.Event"/> this overrides the owning domain. Applied by <see cref="CreateDefaultMessage{T}(T)"/> only.</remarks>
    public string? DomainName { get; set; }

    /// <summary>
    /// Gets or sets the optional destination override.
    /// </summary>
    public string? Destination { get; set; }

    /// <summary>
    /// Gets or sets an optional publisher resolver, for keyed or custom publisher registrations.
    /// </summary>
    /// <remarks>The resolver receives the execution context service provider, falling back to the HTTP request services where unavailable. A configured resolver must return a publisher.</remarks>
    public Func<IServiceProvider, IEventPublisher>? PublisherProvider { get; set; }

    /// <summary>
    /// Gets or sets the optional operation timing configuration.
    /// </summary>
    /// <remarks>On an HTTP endpoint, tracking is enabled by <see cref="CoreEx.AspNetCore.Mvc.PublishTrackingAttribute"/>; this property only tunes the start timeout and expiry. Supplying this property on an endpoint without that metadata is an error. When calling <see cref="CoreEx.AspNetCore.Abstractions.WebApi{TResult}.PublishAsync{TRequest}(Microsoft.AspNetCore.Http.HttpRequest, WebApiPublishOptions{TRequest}, System.Threading.CancellationToken)"/> without a routed endpoint, a non-null value explicitly enables tracking.</remarks>
    public WebApiPublishOperationOptions? Operation { get; set; }

    /// <summary>
    /// Creates the default message for the <paramref name="value"/> using the configured <see cref="MessageType"/>, <see cref="Action"/> and <see cref="DomainName"/>.
    /// </summary>
    /// <typeparam name="T">The message payload type.</typeparam>
    /// <param name="value">The message payload.</param>
    /// <returns>The new <see cref="EventData"/>.</returns>
    /// <remarks>The returned message may be further customized before it is returned from a publishing callback or factory.</remarks>
    public EventData CreateDefaultMessage<T>(T value)
    {
        var message = MessageType switch
        {
            CoreEx.Events.MessageType.Command => new EventData { MessageType = CoreEx.Events.MessageType.Command },
            CoreEx.Events.MessageType.Event => new EventData(),
            _ => throw new NotSupportedException($"The {nameof(MessageType)} '{MessageType}' is not supported for Web API publishing.")
        };

        if (string.IsNullOrEmpty(Action))
            message.WithAction(MessageType == CoreEx.Events.MessageType.Command ? EventAction.Publish : EventAction.Published);
        else
            message.WithAction(Action);

        if (!string.IsNullOrEmpty(DomainName))
            message.WithDomain(DomainName);

        return message.WithValue(value);
    }
}
