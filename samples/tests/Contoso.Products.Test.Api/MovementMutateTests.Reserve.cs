namespace Contoso.Products.Test.Api;

public partial class MovementMutateTests 
{
    [Test]
    public void Reserve_ProductNotFound()
    {
        var req = new MovementRequest
        {
            Id = 671953.ToGuid().ToString(),
            Products = new()
            {
                { 9999.ToGuid().ToString(), new MovementRequestProduct { Quantity = 3, UnitOfMeasureCode = "EA" } }
            }
        };

        Test.Http()
            .Run(HttpMethod.Post, "/api/inventory/reserve", req)
            .AssertBadRequest()
            .AssertErrors(new ApiError("products.0000270f-0000-0000-0000-000000000000", "Product was not found."));
    }

    [Test]
    public void Reserve_InsufficientStock()
    {
        var p1 = 28.ToGuid().ToString();
        var p2 = 16.ToGuid().ToString();

        var req = new MovementRequest
        {
            Id = 671953.ToGuid().ToString(),
            Products = new()
            {
                { p1, new MovementRequestProduct { Quantity = 3, UnitOfMeasureCode = "EA" } },
                { p2, new MovementRequestProduct { Quantity = 100, UnitOfMeasureCode = "EA" } }
            }
        };

        Test.Http()
            .Run(HttpMethod.Post, "/api/inventory/reserve", req)
            .AssertBadRequest()
            .AssertProblemDetailsTitle($"Product '{p2}' does not have sufficient quantity on hand.");
    }

    [Test]
    public void Reserve_Success()
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
            Id = 671953.ToGuid().ToString(),
            Products = new()
            {
                { p1, new MovementRequestProduct { Quantity = 3, UnitOfMeasureCode = "EA" } },
                { 14.ToGuid().ToString(), new MovementRequestProduct { Quantity = 0, UnitOfMeasureCode = "EA" } },
                { p2, new MovementRequestProduct { Quantity = 1, UnitOfMeasureCode = "EA" } }
            }
        };

        Test.Http<Movement[]>()
            .ExpectPostgresOutboxEvents(e => e.AssertAllFromJsonResource("MovementMutateTests.Reserve_Success.event.json"))
            .Run(HttpMethod.Post, "/api/inventory/reserve", req)
            .AssertOK()
            .Value.Should().HaveCount(2);

        // Assert - on-hand quantities should be reduced by reserved amounts.
        Test.Http<decimal>()
            .Run(HttpMethod.Get, $"/api/products/{p1}/on-hand")
            .AssertOK()
            .Value.Should().Be(q1 - 3);

        Test.Http<decimal>()
            .Run(HttpMethod.Get, $"/api/products/{p2}/on-hand")
            .AssertOK()
            .Value.Should().Be(q2 - 1);
    }
}