namespace CoreEx.Database.Outbox;

/// <summary>
/// Provides the standard <see cref="DatabaseOutboxRelayBase{TDatabase, TSelf}"/> invoker functionality.
/// </summary>
/// <typeparam name="TDatabase">The <see cref="IDatabase"/> <see cref="Type"/>.</typeparam>
/// <typeparam name="TSelf">The <see cref="DatabaseOutboxRelayBase{TDatabase, TSelf}"/> instance (self) <see cref="Type"/>.</typeparam>
[InvokerName("CoreEx.Database.Outbox.Relay")]
public class DatabaseOutboxRelayInvoker<TDatabase, TSelf> : InvokerBase<DatabaseOutboxRelayBase<TDatabase, TSelf>> where TDatabase : IDatabase where TSelf : DatabaseOutboxRelayBase<TDatabase, TSelf> { }