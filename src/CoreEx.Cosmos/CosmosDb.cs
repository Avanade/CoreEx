namespace CoreEx.Cosmos;

/// <summary>
/// Provides the core <see href="https://learn.microsoft.com/en-us/azure/cosmos-db/">Azure Cosmos DB</see> <see cref="ICosmosDb"/> functionality.
/// </summary>
/// <remarks>The <see cref="OnCosmosException(CosmosException)"/> converts the following pre-determined <see cref="CosmosException.StatusCode"/> values:
/// <list type="bullet">
///   <item><see cref="HttpStatusCode.NotFound"/> (<c>404</c>) -> <see cref="NotFoundException"/>.</item>
///   <item><see cref="HttpStatusCode.Conflict"/> (<c>409</c>) -> <see cref="DuplicateException"/>.</item>
///   <item><see cref="HttpStatusCode.PreconditionFailed"/> (<c>412</c>) -> <see cref="ConcurrencyException"/>.</item>
/// </list>
/// <para>The <see cref="CosmosClient"/> is <b>not</b> created/owned by <see cref="CosmosDb"/>; it is expected to be resolved from dependency injection (typically registered via Aspire's
/// <c>builder.AddAzureCosmosClient("Cosmos")</c>) and shared across the application.</para></remarks>
public class CosmosDb : ICosmosDb
{
    private readonly ConcurrentDictionary<string, Container> _containers = new();
    private readonly ConcurrentDictionary<string, object> _modelContainers = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosDb"/> class.
    /// </summary>
    /// <param name="client">The <see cref="CosmosClient"/>.</param>
    /// <param name="databaseId">The <see cref="Microsoft.Azure.Cosmos.Database"/> identifier.</param>
    /// <param name="invoker">The optional <see cref="CosmosDbInvoker"/>.</param>
    /// <param name="options">The optional <see cref="CosmosDbOptions"/> (typically a singleton service or statically declared).</param>
    /// <param name="executionContext">The optional <see cref="ExecutionContext"/>.</param>
    /// <param name="logger">The optional <see cref="ILogger"/>.</param>
    public CosmosDb(CosmosClient client, string databaseId, CosmosDbInvoker? invoker = null, CosmosDbOptions? options = null, ExecutionContext? executionContext = null, ILogger<CosmosDb>? logger = null)
    {
        Client = client.ThrowIfNull();
        Database = Client.GetDatabase(databaseId.ThrowIfNull());
        Invoker = invoker ?? CosmosDbInvoker.Default;
        Options = options ?? new CosmosDbOptions();
        ExecutionContext = executionContext ?? ExecutionContext.Current;
        Logger = logger;
    }

    /// <inheritdoc/>
    public CosmosClient Client { get; }

    /// <inheritdoc/>
    public Database Database { get; }

    /// <inheritdoc/>
    public CosmosDbInvoker Invoker { get; set => field = value.ThrowIfNull(); }

    /// <inheritdoc/>
    public CosmosDbArgs DbArgs => Options.Args;

    /// <inheritdoc/>
    public ExecutionContext ExecutionContext { get; }

    /// <inheritdoc/>
    public CosmosDbOptions Options { get; }

    /// <summary>
    /// Gets the <see cref="ILogger"/>.
    /// </summary>
    protected ILogger? Logger { get; }

    /// <inheritdoc/>
    public CosmosDbTransaction? CurrentTransaction { get; private set; }

    /// <inheritdoc/>
    public void UseTransaction(CosmosDbTransaction? transaction) => CurrentTransaction = transaction;

    /// <inheritdoc/>
    public Container GetContainer(string containerId) => _containers.GetOrAdd(containerId.ThrowIfNull(), cid => Database.GetContainer(cid));

    /// <inheritdoc/>
    public CosmosDbContainer<TModel> Container<TModel>(string containerId, Action<CosmosDbModelOptions<TModel>>? configure = null) where TModel : class, IEntityKey, new()
        => (CosmosDbContainer<TModel>)_modelContainers.GetOrAdd(containerId.ThrowIfNull(), cid =>
        {
            var options = Options.GetOrAddModelOptions<TModel>(cid);
            configure?.Invoke(options);
            return new CosmosDbContainer<TModel>(this, GetContainer(cid), options);
        });

    /// <inheritdoc/>
    public Exception? HandleCosmosException(CosmosException cex) => OnCosmosException(cex.ThrowIfNull());

    /// <summary>
    /// Provides the <see cref="CosmosException"/> handling as a result of <see cref="HandleCosmosException(CosmosException)"/>.
    /// </summary>
    /// <param name="cex">The <see cref="CosmosException"/>.</param>
    /// <returns>The converted <see cref="Exception"/> where handled; otherwise, <see langword="null"/>.</returns>
    protected virtual Exception? OnCosmosException(CosmosException cex) => cex.StatusCode switch
    {
        HttpStatusCode.NotFound => new NotFoundException(null, cex),
        HttpStatusCode.Conflict => new DuplicateException(null, cex),
        HttpStatusCode.PreconditionFailed => new ConcurrencyException(null, cex),
        _ => null
    };
}
