namespace Contoso.E2E.Runner.Scenarios;

/// <summary>
/// Provides scenario setup for performing database migrations (or Cosmos DB provisioning) and refreshing base data for the Products, Shopping, Customers and Orders databases.
/// </summary>
[ScenarioSetUp("Database-Migration", "Database Migration and Base Data Refresh", 1, false)]
public sealed class DatabaseMigrationSetup : IScenario
{
    /// <inheritdoc/>
    public async Task RunAsync(ScenarioContext context)
    {
        await context.StepAsync("Products and Shopping demo catalog bootstrap.", () => DemoCatalogBootstrap.RunAsync(context.TestContext.Config),
            count => $"{count} non-deleted products replicated; baskets and search projections verified.").ConfigureAwait(false);

        // Step 3: Customers database migration.
        await context.StepAsync("Customers database migration.", async () =>
        {
            var cs = context.TestContext.Config.GetValue<string>("E2E:Customers:ConnectionString") ?? throw new InvalidOperationException("E2E:Customers:ConnectionString configuration value is missing.");
            var databaseId = context.TestContext.Config.GetValue<string>("E2E:Customers:DatabaseId") ?? Contoso.Customers.Database.Program.DefaultDatabaseId;

            var args = new CosmosDbProvisionArgs { DatabaseId = databaseId };
            Contoso.Customers.Database.Program.ConfigureProvisionArgs(args);
            args.AddDataResource<Customers.Test.Common.TestData>("read-data.seed.yaml");

            using var client = CosmosDbClientFactory.Create(cs);
            var (Success, Output) = await new CosmosDbProvisioner(client, args).RunAndLogAsync(CosmosDbProvisionCommand.Create | CosmosDbProvisionCommand.ResetAndData).ConfigureAwait(false);
            if (!Success)
                throw new Exception("Database provisioning failed:" + Environment.NewLine + Output);
        }, "Successfully provisioned; base data refreshed.").ConfigureAwait(false);

        // Step 4: Orders database migration.
        await context.StepAsync("Orders database migration.", async () =>
        {
            var cs = context.TestContext.Config.GetValue<string>("E2E:Orders:ConnectionString") ?? throw new InvalidOperationException("E2E:Orders:ConnectionString configuration value is missing.");
            var ma = new MigrationArgs(MigrationCommand.All | MigrationCommand.ResetAndData, cs);
            Contoso.Orders.Database.Program.ConfigureMigrationArgs(ma);

            using var m = new SqlServerMigration(ma);
            var (Success, Output) = await m.MigrateAndLogAsync().ConfigureAwait(false);
            if (!Success)
                throw new Exception("Database migration failed:" + Environment.NewLine + Output);
        }, "Successfully migrated; base data refreshed.").ConfigureAwait(false);
    }
}
