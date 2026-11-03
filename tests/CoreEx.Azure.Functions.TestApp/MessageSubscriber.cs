namespace CoreEx.Azure.Functions.TestApp;

/// <summary>
/// Delegates a key-only command to the application service with explicit failure dispositions.
/// </summary>
[ScopedService, Subscribe("coreex.functions.message.process")]
public sealed class MessageSubscriber : SubscribedBase
{
    private readonly MessageService _service;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="service">The application service.</param>
    public MessageSubscriber(MessageService service)
    {
        _service = service.ThrowIfNull();
        ErrorHandler = new ErrorHandler()
            .Add<ValidationException>(ErrorHandling.DeadLetter)
            .Add<BusinessException>(ErrorHandling.CompleteAsWarning)
            .Add<TimeoutException>(ErrorHandling.Retry)
            .Add<NotSupportedException>(ErrorHandling.Catastrophic);
    }

    /// <inheritdoc/>
    protected override Task<Result> OnReceiveAsync(EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken = default)
        => _service.ProcessAsync(@event.Key.Required(), cancellationToken);
}
