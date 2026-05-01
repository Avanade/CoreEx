namespace CoreEx.Azure.Messaging.ServiceBus;

/// <summary>
/// Enables the standard actions for a <see cref="ServiceBusReceivedMessage"/>.
/// </summary>
public interface IServiceBusMessageActions
{
    /// <summary>
    /// Gets the entity path that represents the source of the message; e.g. topic or queue name.
    /// </summary>
    string EntityPath { get; }

    /// <summary>
    /// Gets the <see cref="Amqp.AmqpAnnotatedMessage"/> representation.
    /// </summary>
    Amqp.AmqpAnnotatedMessage AmqpMessage { get; }

    /// <summary>
    /// Marks the current message as completed, indicating that it has been processed successfully.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    Task CompleteMessageAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Marks the current message as abandoned, indicating that it could not be processed successfully, and records the specified <paramref name="exception"/> as the reason.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> that describes the reason for abandoning the message.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    Task AbandonMessageAsync(Exception exception, CancellationToken cancellationToken);

    /// <summary>
    /// Marks the current message as dead-lettered, indicating that it cannot be processed successfully, and records the specified <paramref name="exception"/> as the reason.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> that describes the reason for dead-lettering the message.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    Task DeadLetterMessageAsync(Exception exception, CancellationToken cancellationToken);
}