namespace CoreEx.Azure.Messaging.ServiceBus.Abstractions;

/// <summary>
/// Provides the base <see cref="IServiceBusMessageActions"/> implementation for Azure Service Bus, including the recording of metrics for each action.
/// </summary>
public abstract class ServiceBusMessageActionsBase : IServiceBusMessageActions
{
    /// <inheritdoc/>
    public abstract string EntityPath { get; }

    /// <inheritdoc/>
    public abstract Amqp.AmqpAnnotatedMessage AmqpMessage { get; }

    /// <inheritdoc/>
    public Task CompleteMessageAsync(CancellationToken cancellationToken)
    {
        ServiceBusMetrics.MessagesReceivedComplete.Add(1, [new(ServiceBusMetrics.SourceTagName, EntityPath)]);
        return OnCompletedMessageAsync(cancellationToken);
    }

    /// <summary>
    /// Marks the current message as completed, indicating that it has been processed successfully.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    protected abstract Task OnCompletedMessageAsync(CancellationToken cancellationToken);

    /// <inheritdoc/>
    public Task AbandonMessageAsync(Exception exception, CancellationToken cancellationToken)
    {
        ServiceBusMetrics.MessagesReceivedAbandoned.Add(1, [new(ServiceBusMetrics.SourceTagName, EntityPath)]);
        return OnAbandonedMessageAsync(exception, cancellationToken);
    }

    /// <summary>
    /// Marks the current message as abandoned, indicating that it could not be processed successfully, and records the specified <paramref name="exception"/> as the reason.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> that describes the reason for abandoning the message.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    protected abstract Task OnAbandonedMessageAsync(Exception exception, CancellationToken cancellationToken);

    /// <inheritdoc/>
    public Task DeadLetterMessageAsync(Exception exception, CancellationToken cancellationToken)
    {
        ServiceBusMetrics.MessagesReceivedDeadLetter.Add(1, [new(ServiceBusMetrics.SourceTagName, EntityPath)]);
        return OnDeadLetteredMessageAsync(exception, cancellationToken);
    }

    /// <summary>
    /// Marks the current message as abandoned, indicating that it could not be processed successfully, and records the specified <paramref name="exception"/> as the reason.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> that describes the reason for abandoning the message.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    protected abstract Task OnDeadLetteredMessageAsync(Exception exception, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the name of the property within the <see cref="ServiceBusReceivedMessage.ApplicationProperties"/> that contains the abandon reason.
    /// </summary>
    public const string AbandonReasonName = "AbandonReason";

    /// <summary>
    /// Gets the text used where no reason is available.
    /// </summary>
    public static LText NoneReasonText { get; } = new LText("CoreEx.Azure.Messaging.ServiceBus.None", "None.");

    /// <summary>
    /// Formats the text for logging purposes, truncating to 512 characters.
    /// </summary>
    /// <param name="text">The text to format.</param>
    /// <param name="default">The default where the <paramref name="text"/> is <see langword="null"/>.</param>
    /// <returns>The formatted text.</returns>
    public static string? FormatText(string? text, string? @default = null) => text?[..Math.Min(text.Length, 512)] ?? @default;
}