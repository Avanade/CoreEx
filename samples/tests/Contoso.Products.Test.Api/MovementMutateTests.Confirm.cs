namespace Contoso.Products.Test.Api;

public partial class MovementMutateTests 
{
    [Test]
    public void Confirm_Success()
    {
        var referenceId = 1000.ToGuid().ToString();

        Test.Http<Movement[]>()
            .ExpectPostgresOutboxEvents(e => e.AssertCount(3))
            .Run(HttpMethod.Post, $"/api/inventory/reservation/{referenceId}/confirm")
            .AssertOK()
            .AssertJsonFromResource("MovementMutateTests.Confirm_Success.res.json", pathsToIgnore: ["etag", "changelog"]);

        // Re-confirm, should result in not found as the reservation has already been confirmed and is no longer pending. 
        Test.Http<ProblemDetails>()
            .ExpectNoSqlServerOutboxEvents()
            .Run(HttpMethod.Post, $"/api/inventory/reservation/{referenceId}/confirm")
            .AssertNotFound()
            .Value!.ErrorCode.Should().Be("pending-reservation-not-found");
    }
}