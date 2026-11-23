namespace CoreEx.Database.Postgres.Test.Unit;

[TestFixture]
public class PostgresOperationScriptMigrationTests : OperationScriptMigrationTestsBase
{
    protected override Type ConsoleType => typeof(CoreEx.Database.Postgres.Test.Console.Program);
    protected override string Schema => "ops";
    protected override string Table => PostgresOperationStore.DefaultTableName;

    protected override string GetConnectionString(string databaseName)
    {
        var configured = new ConfigurationBuilder().AddJsonFile("appsettings.unittest.json").Build().GetValue<string>("Aspire:Npgsql:ConnectionString")
            ?? throw new InvalidOperationException("The PostgreSQL test connection string is missing.");
        return new NpgsqlConnectionStringBuilder(configured) { Database = databaseName, Pooling = false }.ConnectionString;
    }

    protected override async Task VerifyTableExistsAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"SELECT COUNT(*) FROM \"{Schema}\".\"{Table}\"", connection);
        Convert.ToInt64(await command.ExecuteScalarAsync()).Should().Be(0);
    }
}
