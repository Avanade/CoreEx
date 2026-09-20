namespace Contoso.Customers.Test.Api;

public partial class CustomerReadTests : WithApiTester<Contoso.Customers.Api.Program>
{
    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        await Test.DatabaseSetUpAsync("read-data.seed.yaml").ConfigureAwait(false);
        await Test.ClearFusionCacheAsync().ConfigureAwait(false);
    }
}
