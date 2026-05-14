namespace CoreEx.Database.Postgres;

/// <summary>
/// Provides extended <see href="https://www.npgsql.org/">Npgsql (PostgreSQL)</see> <see cref="DatabaseCommand{TDatabaseArgs, TSelf}"/> capabilities.
/// </summary>
/// <param name="db">The <see cref="IDatabase"/>.</param>
/// <param name="statement">The <see cref="SqlStatement"/>.</param>
/// <remarks>As the underlying <see cref="DbCommand"/> implements <see cref="IDisposable"/> this is only created (and automatically disposed) where executing the command proper.</remarks>
public sealed class PostgresCommand(PostgresDatabase db, SqlStatement statement) : DatabaseCommand<PostgresDatabaseArgs, PostgresCommand>(db, statement) { }