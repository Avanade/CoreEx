namespace CoreEx.Database.SqlServer.Test.Unit;

[TestFixture]
public class SqlServerOperationScriptMigrationTests : OperationScriptMigrationTestsBase
{
    protected override Type ConsoleType => typeof(CoreEx.Database.SqlServer.Test.Console.Program);
    protected override string Schema => "Ops";
    protected override string Table => SqlServerOperationStore.DefaultTableName;

    protected override string GetConnectionString(string databaseName)
    {
        var configured = new ConfigurationBuilder().AddJsonFile("appsettings.unittest.json").Build().GetValue<string>("Aspire:Microsoft:Data:SqlClient:ConnectionString")
            ?? throw new InvalidOperationException("The SQL Server test connection string is missing.");
        return new SqlConnectionStringBuilder(configured) { InitialCatalog = databaseName, Pooling = false }.ConnectionString;
    }

    protected override async Task VerifyTableExistsAsync(string connectionString)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand($"SELECT COUNT(*) FROM [{Schema}].[{Table}]", connection);
        Convert.ToInt64(await command.ExecuteScalarAsync()).Should().Be(0);
    }
}
