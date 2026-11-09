namespace CoreEx.Azure.Functions.TestApp;

/// <summary>
/// Receives validated native JSON messages without CloudEvent conversion or subscribed dispatch.
/// </summary>
[ScopedService]
public sealed class NativeMessageSubscriber : ServiceBusSubscriberBase<NativeMessageRequest>
{
    private readonly MessageService _service;

    /// <summary>
    /// Initializes a new instance with explicit poison-message and application-failure dispositions.
    /// </summary>
    /// <param name="service">The application service.</param>
    /// <param name="logger">The subscriber logger.</param>
    public NativeMessageSubscriber(MessageService service, ILogger<ServiceBusSubscriberBase> logger) : base(logger)
    {
        _service = service.ThrowIfNull();
        PropagateBaggage = true;
        InvalidDataHandling = ErrorHandling.DeadLetter;
        ErrorHandler = new ErrorHandler()
            .Add<ValidationException>(ErrorHandling.DeadLetter)
            .Add<BusinessException>(ErrorHandling.CompleteAsWarning)
            .Add<TimeoutException>(ErrorHandling.Retry)
            .Add<NotSupportedException>(ErrorHandling.Catastrophic);
    }

    /// <inheritdoc/>
    public override IValidator<NativeMessageRequest>? ValueValidator => NativeMessageRequestValidator.Default;

    /// <inheritdoc/>
    protected override Task<Result> OnReceiveAsync(NativeMessageRequest value, ServiceBusReceivedMessage message, EventSubscriberArgs args, CancellationToken cancellationToken = default)
    {
        if (message.ApplicationProperties.TryGetValue("test-tracing", out var observe) && observe is true)
            ServiceBusTraceProbe.RecordProcessing(message.MessageId);

        return _service.ProcessAsync(value.Outcome.Required(), cancellationToken);
    }
}
