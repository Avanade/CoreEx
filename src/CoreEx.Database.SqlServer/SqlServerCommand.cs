namespace CoreEx.Database.SqlServer;

/// <summary>
/// Provides extended <see href="https://learn.microsoft.com/en-us/sql/">SQL Server</see> <see cref="DatabaseCommand{TDatabaseArgs, TSelf}"/> capabilities.
/// </summary>
/// <param name="db">The <see cref="IDatabase"/>.</param>
/// <param name="statement">The <see cref="SqlStatement"/>.</param>
/// <remarks>As the underlying <see cref="DbCommand"/> implements <see cref="IDisposable"/> this is only created (and automatically disposed) where executing the command proper.</remarks>
public sealed class SqlServerCommand(SqlServerDatabase db, SqlStatement statement) : DatabaseCommand<SqlServerDatabaseArgs, SqlServerCommand>(db, statement) { }