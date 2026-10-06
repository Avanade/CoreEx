namespace Contoso.Customers.Database;

/// <summary>
/// Provisions the Customers Cosmos DB database and containers, and imports the seed data; the Cosmos DB equivalent of the relational domains' <c>*.Database</c> (DbEx) projects.
/// </summary>
/// <remarks>Run using <c>dotnet run -- All</c> (or <c>DropAndAll</c>; <c>ResetAndData</c> requires the database to already exist) (see <c>--help</c> for all commands). The container identifiers declared here must match those used by <c>CustomersCosmosDb</c>.</remarks>
public class Program
{
    /// <summary>
    /// The default (local emulator) connection string; override using <c>-cs</c>.
    /// </summary>
    public const string DefaultConnectionString = "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

    /// <summary>
    /// The default database identifier; override using <c>-d</c>.
    /// </summary>
    public const string DefaultDatabaseId = "contoso";

    /// <summary>
    /// The program entry point.
    /// </summary>
    public static Task<int> Main(string[] args) => CosmosDbConsole.Create<Program>(DefaultConnectionString, DefaultDatabaseId).Configure(c => ConfigureProvisionArgs(c.Args)).RunAsync(args);

    /// <summary>
    /// Declares the containers (and embedded <c>Data</c> seed resources); shared by the console and the tests so both provision identically.
    /// </summary>
    /// <param name="args">The <see cref="CosmosDbProvisionArgs"/>.</param>
    /// <returns>The <paramref name="args"/>.</returns>
    public static CosmosDbProvisionArgs ConfigureProvisionArgs(CosmosDbProvisionArgs args) => args
        .AddAssembly<Program>()
        .Container("customers", configure: cp =>
        {
            // CustomerQueryArgsConfig's "LastName" order-by field is configured WithAlwaysInclude() (always appended, in its own default ascending direction, regardless of what the caller actually
            // requested) - so ordering by "FirstName" always produces a two-property ORDER BY (e.g. "firstName DESC, lastName ASC"), which Cosmos DB rejects outright unless a matching composite index
            // exists. Both direction combinations actually reachable via the API ($orderby=firstname[,desc]) are indexed.
            cp.IndexingPolicy.CompositeIndexes.Add(
            [
                new() { Path = "/firstName", Order = CompositePathSortOrder.Ascending },
                new() { Path = "/lastName", Order = CompositePathSortOrder.Ascending }
            ]);
            cp.IndexingPolicy.CompositeIndexes.Add(
            [
                new() { Path = "/firstName", Order = CompositePathSortOrder.Descending },
                new() { Path = "/lastName", Order = CompositePathSortOrder.Ascending }
            ]);
        })
        .ReferenceDataContainer("ref-data");
}
