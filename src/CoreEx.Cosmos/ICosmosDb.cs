namespace CoreEx.Cosmos;

/// <summary>
/// Enables the core <see href="https://learn.microsoft.com/en-us/azure/cosmos-db/">Azure Cosmos DB</see> access capabilities.
/// </summary>
public interface ICosmosDb
{
    /// <summary>
    /// Gets the underlying <see cref="Microsoft.Azure.Cosmos.CosmosClient"/>.
    /// </summary>
    CosmosClient Client { get; }

    /// <summary>
    /// Gets the <see cref="Microsoft.Azure.Cosmos.Database"/>.
    /// </summary>
    Database Database { get; }

    /// <summary>
    /// Gets the <see cref="CosmosDbInvoker"/>.
    /// </summary>
    CosmosDbInvoker Invoker { get; }

    /// <summary>
    /// Gets the default <see cref="CosmosDbArgs"/>.
    /// </summary>
    CosmosDbArgs DbArgs { get; }

    /// <summary>
    /// Gets the <see cref="CoreEx.ExecutionContext"/>.
    /// </summary>
    ExecutionContext ExecutionContext { get; }

    /// <summary>
    /// Gets the <see cref="CosmosDbOptions"/>.
    /// </summary>
    CosmosDbOptions Options { get; }

    /// <summary>
    /// Gets the ambient ("current") <see cref="CosmosDbTransaction"/> for an active <see cref="CosmosDbUnitOfWork"/>, where one is in scope; otherwise, <see langword="null"/>.
    /// </summary>
    /// <remarks>Mirrors <c>IDatabase.CurrentTransaction</c> — <see cref="CosmosDbContainer{TModel}"/>'s Create/Update/Delete operations check this to transparently enlist into the ambient batch instead of
    /// executing directly, exactly like a SQL repository enlists into an open ADO.NET transaction unchanged. Set via <see cref="UseTransaction(CosmosDbTransaction?)"/>, only by <see cref="CosmosDbUnitOfWork"/>.</remarks>
    CosmosDbTransaction? CurrentTransaction { get; }

    /// <summary>
    /// Sets (or clears, where <see langword="null"/>) the ambient ("current") <see cref="CosmosDbTransaction"/> (see <see cref="CurrentTransaction"/>).
    /// </summary>
    /// <param name="transaction">The <see cref="CosmosDbTransaction"/>; <see langword="null"/> to clear.</param>
    void UseTransaction(CosmosDbTransaction? transaction);

    /// <summary>
    /// Gets (creates and caches) the underlying <see cref="Container"/> for the specified <paramref name="containerId"/>.
    /// </summary>
    /// <param name="containerId">The <see cref="Container"/> identifier.</param>
    /// <returns>The <see cref="Container"/>.</returns>
    Container GetContainer(string containerId);

    /// <summary>
    /// Gets (creates and caches) the <see cref="CosmosDbContainer{TModel}"/> for the specified <paramref name="containerId"/>.
    /// </summary>
    /// <typeparam name="TModel">The model <see cref="Type"/>.</typeparam>
    /// <param name="containerId">The <see cref="Container"/> identifier.</param>
    /// <param name="configure">An optional action to configure the corresponding <see cref="CosmosDbModelOptions{TModel}"/>; only invoked the first time the <see cref="CosmosDbContainer{TModel}"/> for the
    /// <paramref name="containerId"/> is created (i.e. it is cached/reused on subsequent calls).</param>
    /// <returns>The <see cref="CosmosDbContainer{TModel}"/>.</returns>
    CosmosDbContainer<TModel> Container<TModel>(string containerId, Action<CosmosDbModelOptions<TModel>>? configure = null) where TModel : class, IEntityKey, new();

    /// <summary>
    /// Handles the <see cref="CosmosException"/> converting to a corresponding CoreEx <see cref="Exception"/> (where applicable).
    /// </summary>
    /// <param name="cex">The <see cref="CosmosException"/>.</param>
    /// <returns>The converted <see cref="Exception"/> where handled; otherwise, <see langword="null"/> indicating that the exception is unexpected and will continue to be thrown/bubbled as-is.</returns>
    /// <remarks>Provides an opportunity to inspect and convert the exception before it continues to bubble.</remarks>
    Exception? HandleCosmosException(CosmosException cex);
}
