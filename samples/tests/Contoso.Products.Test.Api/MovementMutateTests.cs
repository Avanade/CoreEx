namespace Contoso.Products.Test.Api;

public partial class MovementMutateTests : WithApiTester<Contoso.Products.Api.Program>
{
    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        await Test.MigratePostgresDataAsync<TestData>(["mutate-data.seed.yaml"], DbMigration.ConfigureMigrationArgs).ConfigureAwait(false);
        await Test.ClearFusionCacheAsync().ConfigureAwait(false);

        Test.UseExpectedPostgresOutboxPublisher();
    }
}