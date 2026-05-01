namespace Contoso.Products.Test.Api;

public partial class MovementMutateTests : WithApiTester<Contoso.Products.Api.Program>
{
    [Test]
    public void Adjust_Success()
    {
        // Arrange - get current on-hand quantities for the products to assert against after reservation
        var p1 = 28.ToGuid().ToString();
        var p2 = 16.ToGuid().ToString();

        var q1 = Test.Http<decimal>()
            .Run(HttpMethod.Get, $"/api/products/{p1}/on-hand")
            .AssertOK()
            .Value;

        var q2 = Test.Http<decimal>()
            .Run(HttpMethod.Get, $"/api/products/{p2}/on-hand")
            .AssertOK()
            .Value;

        // Act - reserve inventory for the products.
        var req = new MovementRequest
        {
            Id = 791632.ToGuid().ToString(),
            Products = new()
            {
                { p1, new MovementRequestProduct { Quantity = 33, UnitOfMeasureCode = "EA" } },
                { 14.ToGuid().ToString(), new MovementRequestProduct { Quantity = 0, UnitOfMeasureCode = "EA" } },
                { p2, new MovementRequestProduct { Quantity = 11, UnitOfMeasureCode = "EA" } }
            }
        };

        Test.Http<Movement[]>()
            .ExpectSqlServerOutboxEvents(e => e.AssertAllFromJsonResource("MovementMutateTests.Adjust_Success.event.json"))
            .Run(HttpMethod.Post, "/api/inventory/adjust", req)
            .AssertOK()
            .Value.Should().HaveCount(2);

        // Assert - on-hand quantities should be reduced by reserved amounts.
        Test.Http<decimal>()
            .Run(HttpMethod.Get, $"/api/products/{p1}/on-hand")
            .AssertOK()
            .Value.Should().Be(33);

        Test.Http<decimal>()
            .Run(HttpMethod.Get, $"/api/products/{p2}/on-hand")
            .AssertOK()
            .Value.Should().Be(11);
    }
}