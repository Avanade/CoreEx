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
    /// <remarks>The dead-letter error description is intentionally left unset; the exception (including its stack trace) is already captured in full via the standard logging (see <see cref="ErrorHandler"/>),
    /// so duplicating it onto broker-persisted message metadata (visible to anyone with dead-letter subqueue read access, which may be a broader or different audience than log readers) would add exposure without
    /// adding diagnostic value.</remarks>
    protected override Task OnDeadLetteredMessageAsync(Exception exception, CancellationToken cancellationToken)
        => _args.DeadLetterMessageAsync(_args.Message, FormatText(exception.Message, NoneReasonText), null, cancellationToken);
}