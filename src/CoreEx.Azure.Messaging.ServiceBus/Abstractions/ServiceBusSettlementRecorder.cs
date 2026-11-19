namespace CoreEx.Azure.Messaging.ServiceBus.Abstractions;

/// <summary>
/// Decorates an <see cref="IServiceBusMessageActions"/> to record the settlement actually applied, so that a once-per-delivery <see cref="SubscriberDisposition"/> can be created.
/// </summary>
/// <param name="actions">The underlying <see cref="IServiceBusMessageActions"/>.</param>
internal sealed class ServiceBusSettlementRecorder(IServiceBusMessageActions actions) : IServiceBusMessageActions
{
    private readonly IServiceBusMessageActions _actions = actions.ThrowIfNull();

    /// <summary>
    /// Gets the settlement successfully applied, where any.
    /// </summary>
    public SubscriberSettlement? Applied { get; private set; }

    /// <summary>
    /// Gets the settlement last attempted, where any.
    /// </summary>
    public SubscriberSettlement? Attempted { get; private set; }

    /// <summary>
    /// Gets the settlement error, where any.
    /// </summary>
    public Exception? SettlementError { get; private set; }

    /// <inheritdoc/>
    public string EntityPath => _actions.EntityPath;

    /// <inheritdoc/>
    public Amqp.AmqpAnnotatedMessage AmqpMessage => _actions.AmqpMessage;

    /// <inheritdoc/>
    public Task CompleteMessageAsync(CancellationToken cancellationToken) => RecordAsync(SubscriberSettlement.Completed, () => _actions.CompleteMessageAsync(cancellationToken));

    /// <inheritdoc/>
    public Task AbandonMessageAsync(Exception exception, CancellationToken cancellationToken) => RecordAsync(SubscriberSettlement.Abandoned, () => _actions.AbandonMessageAsync(exception, cancellationToken));

    /// <inheritdoc/>
    public Task DeadLetterMessageAsync(Exception exception, CancellationToken cancellationToken) => RecordAsync(SubscriberSettlement.DeadLettered, () => _actions.DeadLetterMessageAsync(exception, cancellationToken));

    /// <summary>
    /// Records the outcome of the settlement attempt; any settlement exception is rethrown unchanged.
    /// </summary>
    private async Task RecordAsync(SubscriberSettlement settlement, Func<Task> settleAsync)
    {
        Attempted = settlement;
        try
        {
            await settleAsync().ConfigureAwait(false);
            Applied = settlement;
        }
        catch (Exception ex)
        {
            SettlementError = ex;
            throw;
        }
    }

    /// <summary>
    /// Creates the <see cref="SubscriberDisposition"/> from the recorded settlement.
    /// </summary>
    /// <param name="error">The final processing error, where any.</param>
    /// <param name="isCancelled">Indicates whether processing ended due to the receive cancellation token.</param>
    /// <param name="receiverPaused">Indicates whether a receiver pause was requested.</param>
    /// <returns>The <see cref="SubscriberDisposition"/>.</returns>
    public SubscriberDisposition CreateDisposition(Exception? error, bool isCancelled = false, bool receiverPaused = false) => new()
    {
        Settlement = SettlementError is not null ? SubscriberSettlement.SettlementFailed : Applied ?? SubscriberSettlement.NotSettled,
        AttemptedSettlement = SettlementError is not null ? Attempted : null,
        Classification = isCancelled ? null : Classify(error),
        Error = error,
        SettlementError = SettlementError,
        IsLockLost = SettlementError is ServiceBusException sbex && ServiceBusErrorClassifier.IsLockLost(sbex),
        IsCancelled = isCancelled,
        ReceiverPaused = receiverPaused
    };

    /// <summary>
    /// Gets the <see cref="ErrorHandling"/> classification for the <paramref name="error"/>; <see langword="null"/> for success and <see cref="ErrorHandling.None"/> where unclassified.
    /// </summary>
    private static ErrorHandling? Classify(Exception? error) => error is null ? null : error is IEventSubscriberException esex ? esex.ErrorHandling : ErrorHandling.None;
}
