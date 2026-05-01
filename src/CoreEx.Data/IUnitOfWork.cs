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
}