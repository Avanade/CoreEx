namespace CoreEx.AspNetCore.Publishing;

/// <summary>
/// Configures validation, request mapping, message creation and optional operation tracking for Web API publishing.
/// </summary>
/// <typeparam name="TRequest">The HTTP request type.</typeparam>
/// <typeparam name="TMessage">The published message payload type.</typeparam>
public sealed class WebApiPublishOptions<TRequest, TMessage> : WebApiPublishOptions<TRequest> where TRequest : class where TMessage : class
{
    /// <inheritdoc/>
    internal override Type PublishedType => typeof(TMessage);

    /// <summary>
    /// Gets or sets the required explicit request-to-message mapper.
    /// </summary>
    /// <remarks>Use <see cref="Mapping.Mapper.Create{TSource, TDestination}(Func{TSource, TDestination})"/> for an inline mapping.</remarks>
    public Mapping.IMapper<TRequest, TMessage>? Mapper { get; set; }

    /// <summary>
    /// Gets or sets the optional message factory for the mapped message; by default the mapped message is published as configured by <see cref="WebApiPublishOptions.MessageType"/>, <see cref="WebApiPublishOptions.Action"/> and <see cref="WebApiPublishOptions.DomainName"/>.
    /// </summary>
    public Func<TMessage, EventData>? MessageFactory { get; set; }
}
