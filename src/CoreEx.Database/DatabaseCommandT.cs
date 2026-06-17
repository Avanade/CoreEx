namespace CoreEx.Database;

/// <summary>
/// Provides extended database command capabilities.
/// </summary>
/// <typeparam name="TDatabaseArgs">The <see cref="DatabaseArgs"/> <see cref="Type"/>.</typeparam>
/// <typeparam name="TSelf">The <see cref="DatabaseCommand{TDatabaseArgs, TSelf}"/> <see cref="Type"/>.</typeparam>
/// <param name="db">The <see cref="IDatabase"/>.</param>
/// <param name="statement">The <see cref="SqlStatement"/>.</param>
/// <remarks>As the underlying <see cref="DbCommand"/> implements <see cref="IDisposable"/> this is only created (and automatically disposed) where executing the command proper.</remarks>
public abstract class DatabaseCommand<TDatabaseArgs, TSelf>(IDatabase db, SqlStatement statement) : DatabaseCommand(db, statement) 
    where TDatabaseArgs : DatabaseArgs
    where TSelf : DatabaseCommand<TDatabaseArgs, TSelf>
{
    /// <summary>
    /// Gets the <typeparamref name="TDatabaseArgs"/> for the command.
    /// </summary>
    /// <remarks>Defaults to the underlying <see cref="IDatabase.DbArgs"/>.
    /// <para>See also <see cref="WithDbArgs(TDatabaseArgs)"/>.</para></remarks>
    public new TDatabaseArgs DbArgs { get; protected set; } = (TDatabaseArgs)db.DbArgs;

    /// <summary>
    /// Sets (overrides) the <see cref="DbArgs"/> for the command.
    /// </summary>
    /// <param name="dbArgs">The <typeparamref name="TDatabaseArgs"/>.</param>
    /// <returns></returns>
    public TSelf WithDbArgs(TDatabaseArgs dbArgs)
    {
        DbArgs = dbArgs;
        return (TSelf)this;
    }
}