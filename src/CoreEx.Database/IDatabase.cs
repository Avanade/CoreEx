namespace CoreEx.Database;

/// <summary>
/// Enables database (relational) access.
/// </summary>
public interface IDatabase
{
    /// <summary>
    /// Gets the <see cref="ILogger"/>.
    /// </summary>
    ILogger? Logger { get; }

    /// <summary>
    /// Gets the <see cref="DatabaseInvoker"/>.
    /// </summary>
    DatabaseInvoker Invoker { get; }

    /// <summary>
    /// Gets the default <see cref="DatabaseArgs"/> used where not expliticly specified for an operation.
    /// </summary>
    DatabaseArgs DbArgs { get; }

    /// <summary>
    /// Gets the unique database instance identifier.
    /// </summary>
    string DatabaseId { get; }

    /// <summary>
    /// Gets or sets the <see cref="Entities.DateTimeTransform"/> to be used when retrieving (see <see cref="DatabaseRecord.GetValue{T}(string)"/>) a <see cref="DateTime"/> value from a <see cref="DatabaseRecord"/>.
    /// </summary>
    DateTimeTransform DateTimeTransform { get; set; }

    /// <summary>
    /// Indicates whether to transform a <see cref="DateTimeOffset"/> when adding as a parameter (see <see cref="DatabaseParameterCollection.AddParameter{T}"/> using the <see cref="DateTimeOffset.ToUniversalTime"/>).
    /// </summary>
    /// <remarks>See <see href="https://www.tinybird.co/blog/database-timestamps-timezone"/> for more information. As such, the default will typically be <see langword="true"/>.</remarks>
    bool DateTimeOffsetTransform { get; set; }

    /// <summary>
    /// Gets the names of the convention-based <see cref="Extended.DatabaseColumns"/>.
    /// </summary>
    DatabaseColumns NamedColumns { get; }

    /// <summary>
    /// Gets or sets the <see cref="DatabaseWildcard"/> to enable wildcard replacement.
    /// </summary>
    DatabaseWildcard Wildcard { get; set; }

    /// <summary>
    /// Gets the <see cref="DatabaseColumns.RowVersionName"/> converter.
    /// </summary>
    ISourceConverter<string?> RowVersionConverter { get; }

    /// <summary>
    /// Gets the <see cref="JsonSerializerOptions"/> used to serialize database parameters to JSON.
    /// </summary>
    /// <remarks>See <see cref="DatabaseParameterCollection.AddJsonParameter{T}(string, T)"/>.</remarks>
    JsonSerializerOptions JsonSerializerOptions { get;  }

    /// <summary>
    /// Gets the current <see cref="DbTransaction"/>, where one exists.
    /// </summary>
    DbTransaction? CurrentTransaction { get; }

    /// <summary>
    /// Indicates whether a transaction is currently in progress.
    /// </summary>
    /// <remarks>See <see cref="CurrentTransaction"/>.</remarks>
    bool IsInTransaction { get; }

    /// <summary>
    /// Uses (overrides/resets) the <see cref="IDatabase.CurrentTransaction"/>.
    /// </summary>
    /// <param name="transaction">The <see cref="DbTransaction"/>.</param>
    /// <remarks>Raises the <see cref="UseTransactionChanged"/> event to ensure all interested parties are included (where applicable).</remarks>
    void UseTransaction(DbTransaction? transaction);

    /// <summary>
    /// Raised when the <see cref="UseTransaction(DbTransaction?)"/> results in an underlying <see cref="DbTransaction"/> change.
    /// </summary>
    event EventHandler? UseTransactionChanged;

    /// <summary>
    /// Gets the <see cref="DbConnection"/>.
    /// </summary>
    /// <remarks>Gets the <see cref="DbConnection"/> in its current state; to have it automatically opened use <see cref="GetConnectionAsync(CancellationToken)"/>.</remarks>
    DbConnection Connection { get; }

    /// <summary>
    /// Gets the <see cref="DbConnection"/>.
    /// </summary>
    /// <remarks>The connection will be automatically opened where not already open. The connection will <i>not</i> be closed, that is the responsibility of the caller.</remarks>
    Task<DbConnection> GetConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a <see cref="DatabaseCommand"/> for the <see cref="SqlStatement"/>.
    /// </summary>
    /// <param name="statement">The <see cref="SqlStatement"/>.</param>
    /// <returns>The <see cref="DatabaseCommand"/>.</returns>
    DatabaseCommand Statement(SqlStatement statement);

    /// <summary>
    /// Invoked where a <see cref="DbException"/> has been thrown.
    /// </summary>
    /// <param name="dbex">The <see cref="DbException"/>.</param>
    /// <returns>The <see cref="Exception"/> where handled (converted); otherwise, <see langword="null"/> indicating that the exception is unexpected and will continue to be thrown/bubbled as such.</returns>
    /// <remarks>Provides an opportunity to inspect and convert the exception before it continues to bubble.</remarks>
    Exception? HandleDbException(DbException dbex);

    /// <summary>
    /// Creates a new database parameter.
    /// </summary>
    /// <returns>The <see cref="DbParameter"/>.</returns>
    DbParameter CreateParameter();

    /// <summary>
    /// Gets the next (monotonic counter) save-point name used for nested transactions.
    /// </summary>
    /// <returns>The save-point name.</returns>
    string GetNextSavePointName();
}