namespace CoreEx.Azure.Messaging.ServiceBus.Abstractions;

/// <summary>
/// Provides an implementation of <see cref="IServiceBusMessageActions"/> for the <see cref="ProcessMessageEventArgs"/>.
/// </summary>
/// <param name="args">The <see cref="ProcessMessageEventArgs"/>.</param>
public sealed class ProcessMessageEventArgsActions(ProcessMessageEventArgs args) : ServiceBusMessageActionsBase
{
    private readonly ProcessMessageEventArgs _args = args.ThrowIfNull();

    /// <inheritdoc/>
    public override string EntityPath => _args.EntityPath;

    /// <inheritdoc/>
    public override Amqp.AmqpAnnotatedMessage AmqpMessage => _args.Message.GetRawAmqpMessage();

    /// <inheritdoc/>
    protected override Task OnCompletedMessageAsync(CancellationToken cancellationToken) => _args.CompleteMessageAsync(_args.Message, cancellationToken);

    /// <inheritdoc/>
    protected override Task OnAbandonedMessageAsync(Exception exception, CancellationToken cancellationToken)
        => _args.AbandonMessageAsync(_args.Message, new Dictionary<string, object> { { AbandonReasonName, FormatText(exception.Message, NoneReasonText)! } }, cancellationToken);

    /// <inheritdoc/>
    protected override Task OnDeadLetteredMessageAsync(Exception exception, CancellationToken cancellationToken)
        => _args.DeadLetterMessageAsync(_args.Message, FormatText(exception.Message, NoneReasonText), FormatText(exception.StackTrace), cancellationToken);
}