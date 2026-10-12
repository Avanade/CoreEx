namespace Contoso.Customers.Test.Api;

public partial class CustomerMutateTests : WithApiTester<Contoso.Customers.Api.Program>
{
    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        await Test.MigrateCosmosDataAsync<TestData>(["mutate-data.seed.yaml"], Contoso.Customers.Database.Program.ConfigureProvisionArgs).ConfigureAwait(false);
        await Test.ClearFusionCacheAsync().ConfigureAwait(false);

        Test.UseExpectedCosmosDbOutboxPublisher();
    }
}
