namespace Contoso.Products.Test.Outbox.Relay;

public partial class OtherTests : WithApiTester<Contoso.Products.Outbox.Relay.Program>
{
    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        await Test.MigratePostgresDataAsync<TestData>(DbMigration.ConfigureMigrationArgs).ConfigureAwait(false);
    }
}