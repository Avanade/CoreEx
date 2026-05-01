namespace Contoso.Products.Test.Api;

public partial class MovementMutateTests 
{
    [Test]
    public void Cancel_Success()
    {
        // Arrange - get current on-hand quantities for the products to assert against after cancellation.
        var p1 = 24.ToGuid().ToString();
        var p2 = 28.ToGuid().ToString();

        var q1 = Test.Http<decimal>()
            .Run(HttpMethod.Get, $"/api/products/{p1}/on-hand")
            .AssertOK()
            .Value;

        var q2 = Test.Http<decimal>()
            .Run(HttpMethod.Get, $"/api/products/{p2}/on-hand")
            .AssertOK()
            .Value;

        // Act - cancel the reservation.
        var referenceId = 1001.ToGuid().ToString();

        Test.Http<Movement[]>()
            .ExpectSqlServerOutboxEvents(e => e.AssertCount(2))
            .Run(HttpMethod.Post, $"/api/inventory/reservation/{referenceId}/cancel")
            .AssertOK()
            .AssertJsonFromResource("MovementMutateTests.Cancel_Success.res.json", pathsToIgnore: ["etag", "changelog"]);

        // Assert - re-cancel, should result in not found as the reservation has already been confirmed and is no longer pending. 
        Test.Http<ProblemDetails>()
            .ExpectNoSqlServerOutboxEvents()
            .Run(HttpMethod.Post, $"/api/inventory/reservation/{referenceId}/cancel")
            .AssertNotFound()
            .Value!.ErrorCode.Should().Be("pending-reservation-not-found");

        // Assert - on-hand quantities should have been reversed by reserved amounts.
        Test.Http<decimal>()
            .Run(HttpMethod.Get, $"/api/products/{p1}/on-hand")
            .AssertOK()
            .Value.Should().Be(q1 + 2);

        Test.Http<decimal>()
            .Run(HttpMethod.Get, $"/api/products/{p2}/on-hand")
            .AssertOK()
            .Value.Should().Be(q2 + 1);
    }
}