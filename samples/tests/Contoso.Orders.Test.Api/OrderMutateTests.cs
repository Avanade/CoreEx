namespace Contoso.Orders.Test.Api;

public partial class OrderMutateTests : WithApiTester<Contoso.Orders.Api.Program>
{
    private const string SqlConnectionString = "Data Source=127.0.0.1,1433;Initial Catalog=Contoso;User id=sa;Password=yourStrong(!)Password;TrustServerCertificate=true";

    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        await Test.MigrateSqlServerDataAsync<TestData>(DbMigration.ConfigureMigrationArgs, SqlConnectionString).ConfigureAwait(false);
        await Test.ClearFusionCacheAsync().ConfigureAwait(false);

        Test.UseExpectedSqlServerOutboxPublisher();
    }

    private Contoso.Orders.Contracts.Order CreateOrder(string customerIdPrefix = "CUST")
    {
        var order = new Contoso.Orders.Contracts.Order
        {
            CustomerId = $"{customerIdPrefix}-{Guid.NewGuid():N}"[..21],
            StatusCode = "P"
        };

        return Test.Http<Contoso.Orders.Contracts.Order>()
            .ExpectIdentifier()
            .ExpectETag()
            .ExpectChangeLogCreated()
            .ExpectSqlServerOutboxEvents(e => e.AssertWithValue("contoso", "contoso.orders.order.created.v1"))
            .Run(HttpMethod.Post, "/api/orders", order)
            .AssertCreated()
            .AssertLocationHeader(r => new Uri($"/api/orders/{r!.Id}", UriKind.Relative))
            .Value!;
    }
}