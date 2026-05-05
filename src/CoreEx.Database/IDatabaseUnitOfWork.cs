namespace CoreEx.Database;

/// <summary>
/// Extends the <see cref="IUnitOfWork"/> to provide database-specific functionality.
/// </summary>
public interface IDatabaseUnitOfWork : IUnitOfWork
{
    /// <summary>
    /// Gets the underlying <see cref="IDatabase"/>.
    /// </summary>
    public IDatabase Database { get; }

    /// <summary>
    /// Gets the optional <see cref="IEventPublisher"/> to be used as a <see href="https://microservices.io/patterns/data/transactional-outbox.html">transactional outbox</see>.
    /// </summary>
    public IEventPublisher? Outbox { get; }
}