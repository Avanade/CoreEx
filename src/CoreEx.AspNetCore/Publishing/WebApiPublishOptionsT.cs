namespace CoreEx.AspNetCore.Publishing;

/// <summary>
/// Configures validation, message creation and optional operation tracking for Web API publishing.
/// </summary>
/// <typeparam name="TRequest">The HTTP request type.</typeparam>
public class WebApiPublishOptions<TRequest> : WebApiPublishOptions
{
    /// <summary>
    /// Gets the published message payload type.
    /// </summary>
    internal virtual Type PublishedType => typeof(TRequest);

    /// <summary>
    /// Gets or sets the optional validator.
    /// </summary>
    public IValidator<TRequest>? Validator { get; set; }

    /// <summary>
    /// Gets or sets the optional message factory; by default the request is published as configured by <see cref="WebApiPublishOptions.MessageType"/>, <see cref="WebApiPublishOptions.Action"/> and <see cref="WebApiPublishOptions.DomainName"/>.
    /// </summary>
    public Func<TRequest, EventData>? EventFactory { get; set; }
}
