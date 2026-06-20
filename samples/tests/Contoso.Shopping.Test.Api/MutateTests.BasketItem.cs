namespace Contoso.Shopping.Test.Api;

public partial class MutateTests
{
    [Test]
    public void Basket_Item_Add_New()
    {
        var item = new BasketItemAddRequest { ProductId = 32.ToGuid().ToString(), Quantity = 1.5m };

        var v = Test.Http<Basket>()
            .ExpectChangeLogUpdated()
            .ExpectETag()
            .ExpectJsonFromResource("Basket_Item_Add_New.res.json", [.. _pathsToIgnore, "items[2].id"])
            .ExpectSqlServerOutboxEvents(e => e.AssertWithValue("contoso", "contoso.shopping.basket.updated.v1"))
            .Run(HttpMethod.Post, $"/api/baskets/{3003.ToGuid()}/items", item, r => r.WithIdempotencyKey())
            .AssertOK()
            .Value!;

        Test.Http<Basket>()
            .Run(HttpMethod.Get, $"/api/baskets/{3003.ToGuid()}")
            .AssertOK()
            .AssertValue(v);
    }

    [Test]
    public void Basket_Item_Add_Existing()
    {
        var item = new BasketItemAddRequest { ProductId = 27.ToGuid().ToString(), Quantity = 1m };

        var v = Test.Http<Basket>()
            .ExpectChangeLogUpdated()
            .ExpectETag()
            .ExpectJsonFromResource("Basket_Item_Add_Existing.res.json", _pathsToIgnore)
            .ExpectSqlServerOutboxEvents(e => e.AssertWithValue("contoso", "contoso.shopping.basket.updated.v1"))
            .Run(HttpMethod.Post, $"/api/baskets/{3005.ToGuid()}/items", item, r => r.WithIdempotencyKey())
            .AssertOK()
            .Value!;

        Test.Http<Basket>()
            .Run(HttpMethod.Get, $"/api/baskets/{3005.ToGuid()}")
            .AssertOK()
            .AssertValue(v);
    }

    [Test]
    public void Basket_Item_Update_NotFound()
    {
        var item = new BasketItemUpdateRequest { Quantity = 2m };

        Test.Http()
            .Run(HttpMethod.Put, $"/api/baskets/{3005.ToGuid()}/items/{404.ToGuid()}", item, r => r.WithIfMatch("xxx"))
            .AssertNotFound();
    }

    [Test]
    public void Basket_Item_Update_Scale_Error()
    {
        var item = new BasketItemUpdateRequest { Quantity = 2.3m, ETag = "xxx" };

        Test.Http()
            .Run(HttpMethod.Put, $"/api/baskets/{3005.ToGuid()}/items/{4008.ToGuid()}", item)
            .AssertBadRequest()
            .AssertContentTypeProblemJson()
            .AssertProblemDetailsTitle("Quantity decimal places exceed the specified unit-of-measure (Pair) configuration of 0.");
    }

    [Test]
    public void Basket_Item_Update_Success()
    {
        // Arrange - ensure the item to be updated exists; plus we need the ETag for concurrency control.
        var v = Test.Http<Basket>()
            .Run(HttpMethod.Get, $"/api/baskets/{3005.ToGuid()}")
            .AssertOK()
            .Value!;

        var item = new BasketItemUpdateRequest { Quantity = 6m, ETag = v.Items![0].ETag };

        // Act - update the item quantity.
        v = Test.Http<Basket>()
            .ExpectChangeLogUpdated()
            .ExpectETag()
            .ExpectSqlServerOutboxEvents(e => e.AssertWithValue("contoso", "contoso.shopping.basket.updated.v1"))
            .Run(HttpMethod.Put, $"/api/baskets/{3005.ToGuid()}/items/{4008.ToGuid()}", item, r => r.WithIdempotencyKey())
            .AssertOK()
            .Value!;

        // Assert - the item quantity is updated.
        v.Items![0].Quantity.Should().Be(item.Quantity);

        // Assert - the basket get also reflects the item quantity update.
        Test.Http<Basket>()
            .Run(HttpMethod.Get, $"/api/baskets/{3005.ToGuid()}")
            .AssertOK()
            .AssertValue(v);
    }

    [Test]
    public void Basket_Item_Delete()
    {
        // Arrange - ensure the item to be deleted exists before attempting to delete it, otherwise the test will not be valid
        var v = Test.Http<Basket>()
            .Run(HttpMethod.Get, $"/api/baskets/{3006.ToGuid()}")
            .AssertOK()
            .Value!;

        v.Items.Should().NotBeNull().And.HaveCount(1);
        v.StatusCode.Should().Be(BasketStatus.Active);

        // Act - delete the item.
        var v2 = Test.Http<Basket>()
            .ExpectSqlServerOutboxEvents(e => e.AssertWithValue("contoso", "contoso.shopping.basket.updated.v1"))
            .Run(HttpMethod.Delete, $"/api/baskets/{3006.ToGuid()}/items/{v.Items![0].Id}")
            .AssertOK()
            .Value!;

        // Assert - the item is deleted and the basket is now empty.
        v2.Items.Should().NotBeNull().And.HaveCount(0);
        v2.StatusCode.Should().Be(BasketStatus.Empty);

        // Assert - deleting the item again shows the same result.
        Test.Http<Basket>()
            .ExpectNoSqlServerOutboxEvents()
            .Run(HttpMethod.Delete, $"/api/baskets/{3006.ToGuid()}/items/{v.Items![0].Id}")
            .AssertOK()
            .AssertValue(v2);

        // Assert - the basket get also reflects the item deletion.
        Test.Http<Basket>()
            .Run(HttpMethod.Get, $"/api/baskets/{3006.ToGuid()}")
            .AssertOK()
            .AssertValue(v2);
    }
}