namespace CoreEx.Events.Publishing;

/// <summary>
/// Defines the standardized <i>event</i> adding (<see cref="IEventQueue"/>) and publishing orchestration. 
/// </summary>
/// <remarks>By default, the underlying implementation should support single use; i.e. can only publish once. Once, <see cref="HasBeenPublished"/> the publisher should be immutable, unless explicitly <see cref="Reset"/>.</remarks>
public interface IEventPublisher : IEventQueue
{
    /// <summary>
    /// Indicates whether the event publisher has previously published events.
    /// </summary>
    /// <remarks>Use <see cref="Reset"/> to re-enable the publishing of the events.
    /// <para><i>Note:</i> The internal queue is not automatically emptied in case there is an unexpected error and the publishing needs to be retried.</para></remarks>
    bool HasBeenPublished { get; }

    /// <summary>
    /// Publishes (sends) all previously added (queued) events to the underlying eventing/persistence subsystem.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <remarks>Note that all existing events will remain within the internal queue unless a <see cref="IEventQueue.Clear"/> is explicitly performed.</remarks>
    Task PublishAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets (and clears) the event publisher to re-enable adding and publishing.
    /// </summary>
    /// <remarks>Resets the <see cref="HasBeenPublished"/> to <see langword="false"/>.
    /// <para>All existing events will also be cleared; see <see cref="IEventQueue.Clear"/>.</para></remarks>
    void Reset();

    /// <summary>
    /// Rollback (i.e. dequeue) the specified number of previous <i>Add</i> operations.
    /// </summary>
    /// <param name="count">The number of <i>Add</i> operations to roll back.</param>
    /// <remarks>The rollback will only function where <see cref="HasBeenPublished"/> is <see langword="false"/>.</remarks>
    void Rollback(int count);

    /// <summary>
    /// Gets all destination events currently available.
    /// </summary>
    /// <returns>A <see cref="DestinationEvent"/> array that is a snapshot of the current state; empty where <see cref="IEventQueue.IsEmpty"/>.</returns>
    /// <remarks>This is intended for inspection purposes only; the returned array is a snapshot of the current state. Do not modify the individual elements.</remarks>
    DestinationEvent[] GetEvents();
}