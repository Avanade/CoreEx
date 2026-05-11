namespace Contoso.Orders.Test.Api;

public partial class ReadTests : WithApiTester<Contoso.Orders.Api.Program>
{
    private const string SqlConnectionString = "Data Source=127.0.0.1,1433;Initial Catalog=Contoso;User id=sa;Password=yourStrong(!)Password;TrustServerCertificate=true";

    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        await Test.MigrateSqlServerDataAsync<TestData>(DbMigration.ConfigureMigrationArgs, SqlConnectionString).ConfigureAwait(false);
        await Test.ClearFusionCacheAsync().ConfigureAwait(false);

        Test.UseExpectedSqlServerOutboxPublisher();
    }
}