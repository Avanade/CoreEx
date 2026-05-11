namespace Contoso.E2E.Runner.Scenarios;

/// <summary>
/// Provides scenario setup for performing database migrations and refreshing base data for the Products, Shopping and Orders databases.
/// </summary>
[ScenarioSetUp("Database-Migration", "Database Migration and Base Data Refresh", 1, false)]
public sealed class DatabaseMigrationSetup : IScenario
{
    /// <inheritdoc/>
    public async Task RunAsync(ScenarioContext context)
    {
        // Step 1: Products database migration.
        await context.StepAsync("Products database migration.", async () =>
        {
            var cs = context.TestContext.Config.GetValue<string>("E2E:Products:ConnectionString") ?? throw new InvalidOperationException("E2E:Products:ConnectionString configuration value is missing.");
            var ma = new MigrationArgs(MigrationCommand.All | MigrationCommand.ResetAndData, cs);
            Contoso.Products.Database.Program.ConfigureMigrationArgs(ma);
            ma.AddAssembly<Products.Test.Common.TestData>();

            using var m = new SqlServerMigration(ma);
            var (Success, Output) = await m.MigrateAndLogAsync().ConfigureAwait(false);
            if (!Success)
                throw new Exception("Database migration failed:" + Environment.NewLine + Output);
        }, "Successfully migrated; base data refreshed.").ConfigureAwait(false);

        // Step 2: Shopping database migration.
        await context.StepAsync("Shopping database migration.", async () =>
        {
            var cs = context.TestContext.Config.GetValue<string>("E2E:Shopping:ConnectionString") ?? throw new InvalidOperationException("E2E:Shopping:ConnectionString configuration value is missing.");
            var ma = new MigrationArgs(MigrationCommand.All | MigrationCommand.ResetAndData, cs);
            Contoso.Shopping.Database.Program.ConfigureMigrationArgs(ma);
            ma.AddAssembly<Shopping.Test.Common.TestData>();

            using var m = new SqlServerMigration(ma);
            var (Success, Output) = await m.MigrateAndLogAsync().ConfigureAwait(false);
            if (!Success)
                throw new Exception("Database migration failed:" + Environment.NewLine + Output);
        }, "Successfully migrated; base data refreshed.").ConfigureAwait(false);

        // Step 3: Orders database migration.
        await context.StepAsync("Orders database migration.", async () =>
        {
            var cs = context.TestContext.Config.GetValue<string>("E2E:Orders:ConnectionString") ?? throw new InvalidOperationException("E2E:Orders:ConnectionString configuration value is missing.");
            var ma = new MigrationArgs(MigrationCommand.All | MigrationCommand.ResetAndData, cs);
            Contoso.Orders.Database.Program.ConfigureMigrationArgs(ma);
            ma.AddAssembly<Orders.Test.Common.TestData>();

            using var m = new SqlServerMigration(ma);
            var (Success, Output) = await m.MigrateAndLogAsync().ConfigureAwait(false);
            if (!Success)
                throw new Exception("Database migration failed:" + Environment.NewLine + Output);
        }, "Successfully migrated; base data refreshed.").ConfigureAwait(false);
    }
}