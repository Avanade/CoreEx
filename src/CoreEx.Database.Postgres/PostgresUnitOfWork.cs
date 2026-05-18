namespace CoreEx.Database.Postgres;

/// <summary>
/// Provides the transactional <see cref="IUnitOfWork"/> implementation for <see cref="PostgresDatabase"/> including support for a <see href="https://microservices.io/patterns/data/transactional-outbox.html">transactional outbox</see>.
/// </summary>
/// <param name="database">The <see cref="PostgresDatabase"/>.</param>
/// <param name="outbox">The optional <see cref="IEventPublisher"/>.</param>
/// <param name="invoker">The optional <see cref="PostgresUnitOfWorkInvoker"/> used to orchestrate the <see cref="IUnitOfWork"/> functionality.</param>
public sealed class PostgresUnitOfWork(PostgresDatabase database, IEventPublisher? outbox = null, PostgresUnitOfWorkInvoker? invoker = null) : IDatabaseUnitOfWork
{
    /// <inheritdoc/>
    IDatabase IDatabaseUnitOfWork.Database => Database;

    /// <summary>
    /// Gets the underlying <see cref="PostgresDatabase"/>.
    /// </summary>
    public PostgresDatabase Database { get; } = database.ThrowIfNull();

    /// <summary>
    /// Gets the optional <see cref="IEventPublisher"/> to be used as a <see href="https://microservices.io/patterns/data/transactional-outbox.html">transactional outbox</see>.
    /// </summary>
    /// <remarks>Where provided, the <see cref="IEventPublisher.PublishAsync(CancellationToken)"/> is invoked as part of the underlying <see cref="IUnitOfWork"/> transaction functionality. It is expected that the <see cref="IEventPublisher"/> implementation
    /// uses the same <see cref="PostgresDatabase"/> instance to ensure that the transactional outbox functionality works as expected.</remarks>
    public IEventPublisher? Outbox { get; } = outbox;

    /// <summary>
    /// Gets the underlying <see cref="PostgresUnitOfWorkInvoker"/> used to orchestrate the <see cref="IUnitOfWork"/> functionality.
    /// </summary>
    public PostgresUnitOfWorkInvoker UnitOfWorkInvoker { get; } = invoker ??= PostgresUnitOfWorkInvoker.Default;

    /// <inheritdoc/>
    /// <remarks>The <see cref="Outbox"/> is required to enable.</remarks>
    public bool AreEventsSupported => Outbox is not null;

    /// <inheritdoc/>
    public IEventQueue Events => Outbox ?? throw new NotSupportedException($"A Transaction {nameof(Outbox)} has not been provided to enable {nameof(Events)}.");

    /// <inheritdoc/>
    public Task TransactionAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken = default) => TransactionAsync(Database.DbArgs, work, cancellationToken);

    /// <inheritdoc/>
    public Task<T> TransactionAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken = default) => TransactionAsync(Database.DbArgs, work, cancellationToken);

    /// <inheritdoc/>
    public Task TransactionAsync(IDataArgs args, Func<CancellationToken, Task> work, CancellationToken cancellationToken = default)
        => UnitOfWorkInvoker.InvokeAsync(this, (PostgresDatabaseArgs)args, async (_, _, cancellationToken) => await work(cancellationToken).ConfigureAwait(false), cancellationToken);

    /// <inheritdoc/>
    public Task<T> TransactionAsync<T>(IDataArgs args, Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken = default)
        => UnitOfWorkInvoker.InvokeAsync(this, (PostgresDatabaseArgs)args, async (_, _, cancellationToken) => await work(cancellationToken).ConfigureAwait(false), cancellationToken);
}