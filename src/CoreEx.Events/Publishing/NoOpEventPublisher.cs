namespace CoreEx.Events.Publishing;

/// <summary>
/// Provides a no-operation event publisher; whereby the events are simply swallowed/discarded during final <see cref="OnPublishAsync(DestinationEvent[], CancellationToken)"/>.
/// </summary>
/// <param name="destinationProvider">The optional <see cref="IDestinationProvider"/>.</param>
/// <param name="formatter">The optional <see cref="IEventFormatter"/>.</param>
/// <param name="logger">The optional logger.</param>
public class NoOpEventPublisher(IDestinationProvider? destinationProvider = null, IEventFormatter? formatter = null, ILogger<NoOpEventPublisher>? logger = null) : EventPublisherBase(destinationProvider, formatter, logger)
{
    /// <inheritdoc/>
    protected override Task OnPublishAsync(DestinationEvent[] events, CancellationToken cancellationToken = default) => Task.CompletedTask;
}