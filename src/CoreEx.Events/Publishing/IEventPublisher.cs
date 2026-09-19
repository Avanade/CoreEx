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
    /// Dequeues the specified number of previous <i>Add</i> operations.
    /// </summary>
    /// <param name="count">The number of <i>Add</i> operations to dequeue.</param>
    /// <remarks>This will only function where <see cref="HasBeenPublished"/> is <see langword="false"/>; see <see cref="RollbackAsync(CancellationToken)"/> for the equivalent once already published.</remarks>
    void Dequeue(int count);

    /// <summary>
    /// Rolls back a previous <see cref="PublishAsync(CancellationToken)"/> that has turned out not to have actually taken effect (e.g. a surrounding unit-of-work transaction it was enlisted within
    /// subsequently failed to commit).
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <remarks>A no-op by default (see <see cref="EventPublisherBase"/>) - a real publisher's underlying send is typically already irreversible (or, for a deferred-commit provider such as
    /// <c>CosmosDbEventPublisher</c>, was never actually sent in the first place if the surrounding batch failed to commit), so there is usually nothing to undo. This exists purely so a
    /// test-only decorator (see <c>CoreEx.UnitTesting.Events.EventPublisherDecorator</c>) can be told "the publish you just captured didn't really happen" and correct its own captured state
    /// accordingly - only ever called after <see cref="PublishAsync(CancellationToken)"/> has already completed (i.e. <see cref="HasBeenPublished"/> is <see langword="true"/>), never before.</remarks>
    Task RollbackAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all destination events currently available.
    /// </summary>
    /// <returns>A <see cref="DestinationEvent"/> array that is a snapshot of the current state; empty where <see cref="IEventQueue.IsEmpty"/>.</returns>
    /// <remarks>This is intended for inspection purposes only; the returned array is a snapshot of the current state. Do not modify the individual elements.</remarks>
    DestinationEvent[] GetEvents();
}