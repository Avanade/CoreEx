namespace Contoso.Shopping.Test.Relay;

public partial class OtherTests : WithApiTester<Contoso.Shopping.Relay.Program>
{
    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        await Test.MigrateSqlServerDataAsync<TestData>(["no-data.seed.yaml"], DbMigration.ConfigureMigrationArgs).ConfigureAwait(false);
    }
}
