namespace Contoso.Shopping.Test.Api;

public partial class MutateTests
{
    [Test]
    public void Basket_Create()
    {
        var basket = Test.Http<Basket>()
            .ExpectIdentifier()
            .ExpectChangeLogCreated()
            .ExpectETag()
            .ExpectJsonFromResource("Basket_Create.res.json", _pathsToIgnore)
            .ExpectSqlServerOutboxEvents(e => e.AssertWithValue("contoso", "contoso.shopping.basket.created.v1"))
            .Run(HttpMethod.Post, $"/api/customers/{1004.ToGuid()}/baskets")
            .AssertCreated()
            .AssertLocationHeader(b => new Uri($"/api/baskets/{b!.Id}", UriKind.Relative))
            .Value!;

        Test.Http()
            .Run(HttpMethod.Get, $"/api/baskets/{basket.Id}")
            .AssertOK()
            .AssertValue(basket);
    }

    [Test]
    public void Basket_ApplyDiscount_NotFound()
    {
        Test.Http()
            .Run(HttpMethod.Put, $"/api/baskets/{404.ToGuid()}/apply-discount/save10")
            .AssertNotFound();
    }

    [Test]
    public void Basket_ApplyDiscount_Invalid()
    {
        Test.Http()
            .Run(HttpMethod.Put, $"/api/baskets/{404.ToGuid()}/apply-discount/save100")
            .AssertBadRequest()
            .AssertProblemDetailsTitle("Discount coupon either does not exist or is no longer active.");
    }


    [Test]
    public void Basket_ApplyDiscount_Inactive()
    {
        Test.Http()
            .Run(HttpMethod.Put, $"/api/baskets/{404.ToGuid()}/apply-discount/XMAS2025")
            .AssertBadRequest()
            .AssertProblemDetailsTitle("Discount coupon either does not exist or is no longer active.");
    }

    [Test]
    public void Basket_ApplyDiscount_Success()
    {
        var v = Test.Http<Basket>()
            .Run(HttpMethod.Get, $"/api/baskets/{3006.ToGuid()}")
            .AssertOK()
            .Value!;

        v.Pricing.Should().NotBeNull();
        v.Pricing.DiscountCouponCode.Should().BeNull();
        v.Pricing.DiscountPercentage.Should().Be(0m);
        v.Pricing.DiscountAmount.Should().Be(0m);

        v = Test.Http<Basket>()
            .ExpectChangeLogUpdated()
            .ExpectSqlServerOutboxEvents(e => e.AssertWithValue("contoso", "contoso.shopping.basket.updated.v1"))
            .Run(HttpMethod.Put, $"/api/baskets/{v.Id}/apply-discount/save10")
            .AssertOK()
            .Value!;

        v.Pricing.Should().NotBeNull();
        v.Pricing.DiscountCouponCode.Should().Be("SAVE10");
        v.Pricing.DiscountPercentage.Should().Be(10m);
        v.Pricing.DiscountAmount.Should().NotBe(0);

        Test.Http()
            .Run(HttpMethod.Get, $"/api/baskets/{v.Id}")
            .AssertOK()
            .AssertValue(v);
    }

    [Test]
    public void Basket_Checkout_Success()
    {
        // Arrange - ensure the basket is in the correct state for checkout.
        var v = Test.Http<Basket>()
            .Run(HttpMethod.Get, $"/api/baskets/{3007.ToGuid()}")
            .AssertOK()
            .Value!;

        v.StatusCode.Should().Be(BasketStatus.Active);

        // Arrange - mock the inventory reservation to succeed.
        _mockHttpReserveRequest.WithJsonResourceBody("Basket_Checkout_Success.products.req.json").Respond.With(HttpStatusCode.OK);

        // Act - checkout the basket
        v = Test.Http<Basket>()
            .ExpectChangeLogUpdated()
            .ExpectSqlServerOutboxEvents(e => e.AssertWithValue("contoso", "contoso.shopping.basket.checkedout.v1")
                                               .AssertMetadata("contoso", "contoso.products.reservation.confirm", v.Id))
            .Run(HttpMethod.Post, $"/api/baskets/{v.Id}/checkout")
            .AssertOK()
            .Value!;

        // Assert - the inventory reservation endpoint should have been called with the expected request body.
        _mockHttpReserveRequest.Verify();

        // Assert - the basket should be in the "CheckedOut" state.
        v.StatusCode.Should().Be(BasketStatus.CheckedOut);

        Test.Http()
            .Run(HttpMethod.Get, $"/api/baskets/{v.Id}")
            .AssertOK()
            .AssertValue(v);
    }

    [Test]
    public void Basket_Checkout_Insufficient_Quantity()
    {
        // Arrange - mock the inventory reservation to fail due to insufficient inventory.
        _mockHttpReserveRequest.WithAnyBody()
            .Respond.WithJsonResource("Basket_Checkout_Insufficient_Quantity.products.res.json", HttpStatusCode.BadRequest, System.Net.Mime.MediaTypeNames.Application.ProblemJson);

        // Act / Assert - checkout the basket and assert ProblemDetails.
        Test.Http()
            .ExpectNoSqlServerOutboxEvents()
            .Run(HttpMethod.Post, $"/api/baskets/{3008.ToGuid()}/checkout")
            .AssertBadRequest()
            .AssertContentTypeProblemJson()
            .AssertJsonFromResource("Basket_Checkout_Insufficient_Quantity.products.res.json", "traceid");

        // Assert - the inventory reservation endpoint should have been called with the expected request body.
        _mockHttpReserveRequest.Verify();
    }

    [Test]
    public void Basket_Checkout_Downstream_Validation_Failure()
    {
        // Arrange - mock the inventory reservation to fail due to insufficient inventory.
        _mockHttpReserveRequest.WithAnyBody()
            .Respond.WithJsonResource("Basket_Checkout_Downstream_Validation_Failure.products.res.json", HttpStatusCode.BadRequest, System.Net.Mime.MediaTypeNames.Application.ProblemJson);

        // Act / Assert - checkout the basket and assert ProblemDetails for validation failure.
        Test.Http()
            .ExpectNoSqlServerOutboxEvents()
            .Run(HttpMethod.Post, $"/api/baskets/{3008.ToGuid()}/checkout")
            .AssertInternalServerError()
            .AssertContentTypeProblemJson();
    }

    [Test]
    public void Basket_Checkout_Save_Failure()
    {
        var id = 3008.ToGuid().ToString();

        // Arrange - mock the inventory reservation to always succeed.
        _mockHttpReserveRequest.WithAnyBody().Respond.With(HttpStatusCode.OK);

        // Act / Assert - checkout the basket and assert ProblemDetails.
        Test.Http()
            .OnEventPublish(SqlServerOutboxPublisher.DefaultServiceKey, () => throw new InvalidOperationException("Simulated failure during save; oh no, we all ready reserved inventory!"))
            .ExpectNoSqlServerOutboxEvents()
            .ExpectAzureServiceBusEvents(e => e.AssertMetadata("contoso", "contoso.products.reservation.cancel", id))
            .Run(HttpMethod.Post, $"/api/baskets/{id}/checkout")
            .AssertInternalServerError();

        // Assert - the inventory reservation endpoint should have been called with the expected request body.
        _mockHttpReserveRequest.Verify();

        // Assert - the basket should still be active as the checkout process should have been rolled back.
        var v = Test.Http<Basket>()
            .Run(HttpMethod.Get, $"/api/baskets/{id}")
            .AssertOK()
            .Value!;

        v.StatusCode.Should().Be(BasketStatus.Active);
    }
}