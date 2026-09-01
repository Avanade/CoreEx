namespace CoreEx.Data;

/// <summary>
/// Enables standardized repository-agnostic transactional <i>unit-of-work</i> orchestration.
/// </summary>
/// <remarks>Also, includes <see cref="Events"/> where <see cref="AreEventsSupported">supporting</see> a <see href="https://microservices.io/patterns/data/transactional-outbox.html">transactional outbox</see>.
/// <para>The <see cref="IDataArgs"/> method overloads are enabled for specific advanced/configurable scenarios which would typically be rare. The consumer will need to ensure that the correct <see cref="IDataArgs"/> <see cref="Type"/> is provided.</para>
/// <para>Where implementing this interface the resulting value should be checked to determines if it is an <see cref="IResult"/>; if so, and <see cref="IResult.IsFailure"/> then this should rollback in the same manner
/// that would occur where an <see cref="Exception"/> had been thrown.</para></remarks>
public partial interface IUnitOfWork
{
    /// <summary>
    /// Indicates whether <see cref="Events"/> are supported; i.e. a <see href="https://microservices.io/patterns/data/transactional-outbox.html">transactional outbox</see>.
    /// </summary>
    bool AreEventsSupported { get; }

    /// <summary>
    /// Gets the <see cref="IEventQueue"/> for managing events (<see href="https://microservices.io/patterns/data/transactional-outbox.html">transactional outbox</see>) within the <i>unit-of-work</i>.
    /// </summary>
    /// <remarks>Should throw a <see cref="NotSupportedException"/> where <see cref="AreEventsSupported"/> is <see langword="false"/>.</remarks>
    IEventQueue Events { get; }

    /// <summary>
    /// Orchestrates either a new or <i>flows</i> an existing transaction managing its lifetime and underlying <paramref name="work"/> execution.
    /// </summary>
    /// <param name="work">The work to be executed within the transaction.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    Task TransactionAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken = default);

    /// <summary>
    /// Orchestrates either a new or <i>flows</i> an existing transaction managing its lifetime and underlying <paramref name="work"/> execution that returns a value.
    /// </summary>
    /// <typeparam name="T">The resulting value <see cref="Type"/>.</typeparam>
    /// <param name="work">The work to be executed within the transaction.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resulting value.</returns>
    Task<T> TransactionAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken = default);

    /// <summary>
    /// Orchestrates either a new or <i>flows</i> an existing transaction managing its lifetime and underlying <paramref name="work"/> execution.
    /// </summary>
    /// <param name="args">The <see cref="IDataArgs"/>.</param>
    /// <param name="work">The work to be executed within the transaction.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    Task TransactionAsync(IDataArgs args, Func<CancellationToken, Task> work, CancellationToken cancellationToken = default);

    /// <summary>
    /// Orchestrates either a new or <i>flows</i> an existing transaction managing its lifetime and underlying <paramref name="work"/> execution that returns a value.
    /// </summary>
    /// <typeparam name="T">The resulting value <see cref="Type"/>.</typeparam>
    /// <param name="args">The <see cref="IDataArgs"/>.</param>
    /// <param name="work">The work to be executed within the transaction.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resulting value.</returns>
    Task<T> TransactionAsync<T>(IDataArgs args, Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken = default);

    /// <summary>
    /// Synchronizes the <paramref name="value"/>'s <see cref="IETag.ETag"/> with the true, underlying-store-persisted value for the entity identified by <paramref name="key"/>, where the implementing
    /// provider is unable to make that value available synchronously at the point of mutation (see remarks).
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    /// <param name="key">The <see cref="CompositeKey"/> identifying the mutated entity within this <i>unit-of-work</i>.</param>
    /// <param name="value">The value (typically a mapped <i>contract</i>, not necessarily the same instance/type that was created/updated) whose <see cref="IETag.ETag"/> is to be synchronized.</param>
    /// <remarks>Most providers (e.g. a relational database via <c>IDatabaseUnitOfWork</c>) execute each statement immediately within the open transaction, so a mutated value already carries its true,
    /// final <see cref="IETag.ETag"/> by the time it is returned — for these, an implementation of this method is expected to be a no-op (ignore, not throw); there is nothing to synchronize.
    /// <para>A provider whose only atomic multi-operation primitive defers execution until the unit-of-work completes (e.g. Cosmos DB's <c>TransactionalBatch</c>, executed once at commit time) cannot
    /// give a mutated value its true <see cref="IETag.ETag"/> until that point — such a provider is expected to track mutations by <paramref name="key"/> during the unit-of-work and implement this method
    /// to resolve and assign the real value once available, throwing where <paramref name="key"/> was not part of the most recently completed unit-of-work, or where called before it has completed.</para>
    /// <para><paramref name="key"/> (a value, not an object reference) is used rather than tracking the mutated instance itself, because the value passed here is often a separately mapped <i>contract</i>
    /// (e.g. the value published as an event), not the same object instance the provider mutated — <paramref name="key"/> is expected to survive that mapping boundary even though object identity does not.</para></remarks>
    void SynchronizeETag<T>(CompositeKey key, T value) where T : IETag;
}