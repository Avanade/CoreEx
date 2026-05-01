namespace CoreEx.Database;

/// <summary>
/// Provides the common/base <see cref="IDatabase"/> access functionality.
/// </summary>
/// <typeparam name="TConnection">The <see cref="DbConnection"/> <see cref="Type"/>.</typeparam>
/// <typeparam name="TCommand">The <see cref="DatabaseCommand{TDatabaseArgs, TSelf}"/> <see cref="Type"/>.</typeparam>
/// <typeparam name="TDatabaseArgs">The <see cref="DatabaseArgs"/> <see cref="Type"/>.</typeparam>
/// <param name="provider">The underlying <see cref="DbProviderFactory"/>.</param>
/// <param name="connection">The <typeparamref name="TConnection"/> <see cref="DbConnection"/>.</param>
/// <param name="invoker">The <see cref="DatabaseInvoker"/>.</param>
/// <param name="jsonSerializerOptions">The optional <see cref="JsonSerializerOptions"/>.</param>
/// <param name="logger">The optional <see cref="ILogger"/>.</param>
public abstract class Database<TConnection, TCommand, TDatabaseArgs>(DbProviderFactory provider, TConnection connection, DatabaseInvoker invoker, JsonSerializerOptions? jsonSerializerOptions = null, ILogger<Database<TConnection, TCommand, TDatabaseArgs>>? logger = null) : IDatabase
    where TConnection : DbConnection where TCommand : DatabaseCommand<TDatabaseArgs, TCommand> where TDatabaseArgs : DatabaseArgs, new()
{
    private static readonly TDatabaseArgs _defaultDbArgs = new();
    private static readonly DatabaseColumns _defaultColumns = new();
    private static readonly DatabaseWildcard _defaultWildcard = new();

    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly TConnection _dbConn = connection.ThrowIfNull().ThrowWhen(connection => connection.State != ConnectionState.Closed && connection.State != ConnectionState.Open);
    private int _savePointCounter = 0;

    /// <inheritdoc/>
    public DbProviderFactory Provider { get; } = provider.ThrowIfNull();

    /// <inheritdoc/>
    public string DatabaseId { get; } = Guid.NewGuid().ToString();

    /// <inheritdoc/>
    public ILogger? Logger { get; } = logger ?? ExecutionContext.GetService<ILogger<Database<TConnection, TCommand, TDatabaseArgs>>>();

    /// <inheritdoc/>
    public DatabaseInvoker Invoker { get; } = invoker.ThrowIfNull();

    /// <inheritdoc/>
    DatabaseArgs IDatabase.DbArgs => DbArgs;

    /// <summary>
    /// Gets or sets the default <typeparamref name="TDatabaseArgs"/> used where not explicitly specified for an operation.
    /// </summary>
    public TDatabaseArgs DbArgs { get; set; } = _defaultDbArgs;

    /// <inheritdoc/>
    public DateTimeTransform DateTimeTransform { get; set; } = DateTimeTransform.UseDefault;

    /// <inheritdoc/>
    public bool DateTimeOffsetTransform { get; set; } = true;

    /// <inheritdoc/>
    public DatabaseColumns NamedColumns { get; set; } = _defaultColumns;

    /// <summary>
    /// Gets or sets the <see cref="DatabaseWildcard"/> to enable wildcard replacement.
    /// </summary>
    public DatabaseWildcard Wildcard { get; set; } = _defaultWildcard;

    /// <inheritdoc/>
    public abstract ISourceConverter<string?> RowVersionConverter { get; }

    /// <inheritdoc/>
    public JsonSerializerOptions JsonSerializerOptions { get; } = jsonSerializerOptions ?? JsonDefaults.SerializerOptions;

    /// <inheritdoc/>
    public DbTransaction? CurrentTransaction { get; protected set; }

    /// <inheritdoc/>
    public bool IsInTransaction => CurrentTransaction is not null;

    /// <inheritdoc/>
    public void UseTransaction(DbTransaction? transaction)
    {
        if (CurrentTransaction != transaction)
        {
            CurrentTransaction = transaction;
            UseTransactionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <inheritdoc/>
    public event EventHandler? UseTransactionChanged;

    /// <inheritdoc/>
    DbConnection IDatabase.Connection => _dbConn;

    /// <inheritdoc/>
    async Task<DbConnection> IDatabase.GetConnectionAsync(CancellationToken cancellationToken) => await GetConnectionAsync(cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Gets the <typeparamref name="TConnection"/>.
    /// </summary>
    /// <remarks>The connection is opened on first use.</remarks>
    public async Task<TConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (_dbConn.State == ConnectionState.Open)
            return _dbConn;

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (_dbConn.State == ConnectionState.Open)
                return _dbConn;

            if (_dbConn.State != ConnectionState.Closed)
                throw new InvalidOperationException($"The database connection is in an invalid state: {_dbConn.State}.");

            if (Logger is not null && Logger.IsEnabled(LogLevel.Debug))
                Logger.LogDebug("Opening the database connection. [DatabaseId: {DatabaseId}]", DatabaseId);

            await _dbConn.OpenAsync(cancellationToken).ConfigureAwait(false);
            return _dbConn;
        }
        catch (Exception ex)
        {
            if (Logger is not null && Logger.IsEnabled(LogLevel.Error))
                Logger.LogError(ex, "Error occurred whilst opening the database connection. [DatabaseId: {DatabaseId}]", DatabaseId);

            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc/>
    DatabaseCommand IDatabase.Statement(SqlStatement statement) => Statement(statement);

    /// <summary>
    /// Creates a<typeparamref name="TCommand"/> for the <see cref="SqlStatement"/>.
    /// </summary>
    /// <param name="statement">The <see cref="SqlStatement"/>.</param>
    /// <returns>The <typeparamref name="TCommand"/>.</returns>
    public abstract TCommand Statement(SqlStatement statement);

    /// <inheritdoc/>
    public Exception? HandleDbException(DbException dbex) => OnDbException(dbex);

    /// <summary>
    /// Provides the <see cref="DbException"/> handling as a result of <see cref="HandleDbException(DbException)"/>.
    /// </summary>
    /// <param name="dbex">The <see cref="DbException"/>.</param>
    /// <returns>The <see cref="Exception"/> where handled (converted); otherwise, <see langword="null"/> indicating that the exception is unexpected and will continue to be thrown/bubbled as such.</returns>
    /// <remarks>Provides an opportunity to inspect and convert the exception before it continues to bubble.
    /// <para>Where overriding and the <see cref="DbException"/> is not specifically handled then invoke the base to ensure any standard handling is executed.</para></remarks>
    protected virtual Exception? OnDbException(DbException dbex) => null;

    /// <summary>
    /// Gets the next (monotonic counter) save-point name.
    /// </summary>
    /// <returns>The save-point name.</returns>
    public string GetNextSavePointName()
    {
        var counter = Interlocked.Increment(ref _savePointCounter);
        return $"SP_{counter}";
    }
}