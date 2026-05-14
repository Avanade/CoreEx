namespace CoreEx.Database;

/// <summary>
/// Provides extended database command capabilities.
/// </summary>
/// <param name="db">The <see cref="IDatabase"/>.</param>
/// <param name="statement">The <see cref="SqlStatement"/>.</param>
/// <remarks>As the underlying <see cref="DbCommand"/> implements <see cref="IDisposable"/> this is only created (and automatically disposed) where executing the command proper.</remarks>
public abstract partial class DatabaseCommand(IDatabase db, SqlStatement statement) : IDatabaseParameters<DatabaseCommand>
{
    /// <inheritdoc/>
    public IDatabase Database { get; } = db.ThrowIfNull();

    /// <inheritdoc/>
    public DatabaseParameterCollection Parameters { get; } = new DatabaseParameterCollection(db);

    /// <summary>
    /// Gets the <see cref="SqlStatement"/>.
    /// </summary>
    public SqlStatement Statement { get; } = statement.ThrowIfNull().ThrowWhen(statement => statement == SqlStatement.Indeterminate, "A SQL statement of None is not considered valid for execution.");

    /// <summary>
    /// Gets the <see cref="DatabaseArgs"/> for the command.
    /// </summary>
    /// <remarks>Defaults to the underlying <see cref="IDatabase.DbArgs"/>.</remarks>
    public DatabaseArgs DbArgs { get; protected set; } = db.DbArgs;

    /// <summary>
    /// Creates the corresponding <see cref="DbCommand"/>.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="DbCommand"/>.</returns>
    private async Task<DbCommand> CreateCommandAsync(CancellationToken cancellationToken)
    {
        var conn = await Database.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        var cmd = conn.CreateCommand();

        if (Database.CurrentTransaction is not null)
            cmd.Transaction = Database.CurrentTransaction;

        cmd.CommandType = Statement.CommandType;
        cmd.CommandText = Statement.CommandText;
        cmd.Parameters.AddRange(Parameters.ToArray());
        return cmd;
    }

    /// <summary>
    /// Logs the command type and text at debug level.
    /// </summary>
    private DbCommand LogCommand(DbCommand command)
    {
        if (Database.Logger?.IsEnabled(LogLevel.Debug) is true)
            Database.Logger.LogDebug("Executing DbCommand [CommandType='{CommandType}']:{NewLine}{CommandText}", command.CommandType, Environment.NewLine, command.CommandText);

        return command;
    }
}