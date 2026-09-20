namespace Contoso.Customers.Test.Api;

public partial class CustomerMutateTests : WithApiTester<Contoso.Customers.Api.Program>
{
    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        await Test.DatabaseSetUpAsync("mutate-data.seed.yaml").ConfigureAwait(false);
        await Test.ClearFusionCacheAsync().ConfigureAwait(false);

        Test.UseExpectedCosmosDbOutboxPublisher();
    }
}
