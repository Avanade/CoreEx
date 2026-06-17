namespace CoreEx.Database.Outbox;

/// <summary>
/// Enables the <see cref="RelayAsync(DatabaseOutboxRelayArgs, CancellationToken)"/> operation for performing the <see href="https://microservices.io/patterns/data/transactional-outbox.html">transactional outbox</see> relay.
/// </summary>
public interface IDatabaseOutboxRelay
{
    /// <summary>
    /// Performs the relay operation.
    /// </summary>
    /// <param name="args">The <see cref="DatabaseOutboxRelayArgs"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns><see langword="true"/> indicates that at least one event was relayed; otherwise, <see langword="false"/>.</returns>
    Task<bool> RelayAsync(DatabaseOutboxRelayArgs args, CancellationToken cancellationToken);
}