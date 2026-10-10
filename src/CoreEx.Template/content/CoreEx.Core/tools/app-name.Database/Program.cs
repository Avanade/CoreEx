// #if implement-sqlserver
using CoreEx.Database;
using DbEx.Migration;
using DbEx.SqlServer.Console;

namespace app-name.Database;

/// <summary>Represents the <b>database utilities</b> program.</summary>
public class Program
{
    /// <summary>Main startup.</summary>
    public static Task<int> Main(string[] args) => SqlServerMigrationConsole
        .Create<Program>("Data Source=127.0.0.1,1433;Initial Catalog=domain-name;User id=sa;Password=yourStrong(!)Password;TrustServerCertificate=true")
        .Configure(c => ConfigureMigrationArgs(c.Args))
        .RunAsync(args);

    /// <summary>Configure the <see cref="MigrationArgs"/>.</summary>
    public static MigrationArgs ConfigureMigrationArgs(MigrationArgs args)
    {
        args.AddAssembly<SqlStatement>().AddAssembly<Program>();   // SqlStatement = CoreEx EF code-gen templates; Program = this project's embedded migrations/data. Both REQUIRED — the API tests call ConfigureMigrationArgs directly (not via Main), so the Database assembly must be added here. Do not remove.
        args.DataResetFilterPredicate = ts => ts.Schema == "domain-name";   // Only reset data for the specified schema.
        return args;
    }
}
// #elif implement-postgres
using CoreEx.Database;
using DbEx.Migration;
using DbEx.Postgres.Console;

namespace app-name.Database;

/// <summary>Represents the <b>database utilities</b> program.</summary>
public class Program
{
    /// <summary>Main startup.</summary>
    public static Task<int> Main(string[] args) => PostgresMigrationConsole
        .Create<Program>("Server=127.0.0.1;Database=domain-name-lower;Username=postgres;Password=yourStrong#!Password")
        .Configure(c => ConfigureMigrationArgs(c.Args))
        .RunAsync(args);

    /// <summary>Configure the <see cref="MigrationArgs"/>.</summary>
    public static MigrationArgs ConfigureMigrationArgs(MigrationArgs args)
    {
        args.AddAssembly<SqlStatement>().AddAssembly<Program>();   // SqlStatement = CoreEx EF code-gen templates; Program = this project's embedded migrations/data. Both REQUIRED — the API tests call ConfigureMigrationArgs directly (not via Main), so the Database assembly must be added here. Do not remove.
        args.DataResetFilterPredicate = ts => ts.Schema == "pg-schema";   // Only reset data for the specified schema.
        return args;
    }
}
// #elif implement-cosmos
using CoreEx.Cosmos.Provisioning;

namespace app-name.Database;

/// <summary>Represents the <b>database utilities</b> program; provisions the Cosmos DB database and containers, and imports the seed data (the Cosmos DB equivalent of a relational <b>DbEx</b> migration).</summary>
/// <remarks>Run using <c>dotnet run -- All</c> (or <c>DropAndAll</c>; <c>ResetAndData</c> requires the database to already exist); see <c>--help</c> for all commands. The container identifiers declared here must match those used by <c>domain-nameCosmosDb</c>.</remarks>
public class Program
{
    /// <summary>The default (local emulator) connection string; override using <c>-cs</c>.</summary>
    public const string DefaultConnectionString = "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

    /// <summary>The default database identifier; override using <c>-d</c>.</summary>
    public const string DefaultDatabaseId = "domain-name-lower";

    /// <summary>Main startup.</summary>
    public static Task<int> Main(string[] args) => CosmosDbConsole
        .Create<Program>(DefaultConnectionString, DefaultDatabaseId)
        .Configure(c => ConfigureProvisionArgs(c.Args))
        .RunAsync(args);

    /// <summary>Declares the containers (and embedded <c>Data</c> seed resources); shared by the console and the tests so both provision identically.</summary>
    /// <param name="args">The <see cref="CosmosDbProvisionArgs"/>.</param>
    /// <returns>The <paramref name="args"/>.</returns>
    public static CosmosDbProvisionArgs ConfigureProvisionArgs(CosmosDbProvisionArgs args) => args
        .AddAssembly<Program>()   // Program = this project's embedded data. REQUIRED — the tests call ConfigureProvisionArgs directly (not via Main), so the Database assembly must be added here. Do not remove.
// #if refdata-enabled
        .ReferenceDataContainer("ref-data")
// #endif
// #if outbox-enabled
        .OutboxLeaseContainer()   // The Change Feed Processor lease container shared by the Relay host(s); the relay never creates it (see AddCosmosDbOutboxRelayHostedService).
// #endif
        ;   // Add further containers here (using Container(...)), including any indexing policy required.
}
// #endif
