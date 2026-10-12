namespace Contoso.Customers.Test.Api;

public partial class CustomerReadTests : WithApiTester<Contoso.Customers.Api.Program>
{
    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        await Test.MigrateCosmosDataAsync<TestData>(["read-data.seed.yaml"], Contoso.Customers.Database.Program.ConfigureProvisionArgs).ConfigureAwait(false);
        await Test.ClearFusionCacheAsync().ConfigureAwait(false);
    }
}
