namespace Contoso.Test.Aspire;

public partial class E2ETest
{
    private string _mcpEndpoint = string.Empty;

    [Test]
    public async Task Contoso_Mcp_FullBasketLifecycle()
    {
        Test.Checkpoint("Increase inventory for the seeded, active Product used by the MCP basket scenario.");
        var productId = 28.ToGuid().ToString();
        Test.Http(_productsApi)
            .Run(HttpMethod.Post, "/api/inventory/adjust", new MovementRequest
            {
                Id = Runtime.NewGuid().ToString(),
                Products = new() { { productId, new MovementRequestProduct { Quantity = 5, UnitOfMeasureCode = "EA" } } }
            })
            .AssertOK();

        await using var client = await McpClient.CreateAsync(new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri($"{_mcpEndpoint}/mcp"),
            EnableStandaloneGetStream = false
        })).ConfigureAwait(false);

        var tools = await client.ListToolsAsync().ConfigureAwait(false);
        string[] expectedToolNames =
        [
            "search_products", "get_product", "get_basket", "list_customer_baskets",
            "create_basket", "add_basket_item", "update_basket_item", "remove_basket_item",
            "apply_basket_discount", "update_basket_shipping_address", "checkout_basket"
        ];
        tools.Select(x => x.Name).Should().Contain(expectedToolNames);
        tools.Should().OnlyContain(x => x.ProtocolTool.OutputSchema.HasValue);

        Test.Checkpoint("Read a Product and search the prepared real-model projection through MCP.");
        var product = await CallToolAsync(client, "get_product", new Dictionary<string, object?> { ["productId"] = productId }).ConfigureAwait(false);
        product.GetProperty("id").GetString().Should().Be(productId);
        var productSearch = await CallToolAsync(client, "search_products", new Dictionary<string, object?> { ["searchTerm"] = "something to clean mud off my bike", ["take"] = 1 }).ConfigureAwait(false);
        productSearch.EnumerateArray().Should().ContainSingle().Which.GetProperty("sku").GetString().Should().Be("MUCOFF-NANO-CLEANER");

        Test.Checkpoint("Create a Basket for the seeded MCP demo customer, then read and list it through MCP.");
        var customerId = 16.ToGuid().ToString();
        var basketCreated = await CallToolAsync(client, "create_basket", new Dictionary<string, object?> { ["customerId"] = customerId }).ConfigureAwait(false);
        var basketId = basketCreated.GetProperty("id").GetString() ?? throw new InvalidOperationException("The MCP tool returned a Basket without an ID.");

        (await CallToolAsync(client, "get_basket", new Dictionary<string, object?> { ["basketId"] = basketId }).ConfigureAwait(false)).GetProperty("id").GetString().Should().Be(basketId);
        (await CallToolAsync(client, "list_customer_baskets", new Dictionary<string, object?> { ["customerId"] = customerId }).ConfigureAwait(false))
            .EnumerateArray().Should().Contain(x => x.GetProperty("id").GetString() == basketId);

        Test.Checkpoint("Add a Product, update its quantity, and remove a second item through MCP.");
        var basketAfterAdd = await CallToolAsync(client, "add_basket_item", new Dictionary<string, object?> { ["basketId"] = basketId, ["productId"] = productId, ["quantity"] = 1m }).ConfigureAwait(false);
        var basketItemId = basketAfterAdd.GetProperty("items")[0].GetProperty("id").GetString() ?? throw new InvalidOperationException("The MCP tool returned a Basket item without an ID.");

        var secondProductId = 29.ToGuid().ToString();
        var basketWithSecondItem = await CallToolAsync(client, "add_basket_item", new Dictionary<string, object?> { ["basketId"] = basketId, ["productId"] = secondProductId, ["quantity"] = 1m }).ConfigureAwait(false);
        var secondItemId = basketWithSecondItem.GetProperty("items").EnumerateArray().Single(x => x.GetProperty("productId").GetString() == secondProductId).GetProperty("id").GetString()
            ?? throw new InvalidOperationException("The MCP tool returned the second Basket item without an ID.");

        var updatedBasket = await CallToolAsync(client, "update_basket_item", new Dictionary<string, object?> { ["basketId"] = basketId, ["basketItemId"] = basketItemId, ["quantity"] = 2m }).ConfigureAwait(false);
        updatedBasket.GetProperty("items").EnumerateArray().Single(x => x.GetProperty("id").GetString() == basketItemId).GetProperty("quantity").GetDecimal().Should().Be(2m);
        var basketAfterRemoval = await CallToolAsync(client, "remove_basket_item", new Dictionary<string, object?> { ["basketId"] = basketId, ["basketItemId"] = secondItemId }).ConfigureAwait(false);
        basketAfterRemoval.GetProperty("items").EnumerateArray().Should().NotContain(x => x.GetProperty("productId").GetString() == secondProductId);

        Test.Checkpoint("Apply a coupon, set a shipping address, and check out through MCP.");
        (await CallToolAsync(client, "apply_basket_discount", new Dictionary<string, object?> { ["basketId"] = basketId, ["couponCode"] = "SAVE10" }).ConfigureAwait(false))
            .GetProperty("pricing").GetProperty("discountPercentage").GetDecimal().Should().Be(10m);
        var basketWithAddress = await CallToolAsync(client, "update_basket_shipping_address", new Dictionary<string, object?>
        {
            ["basketId"] = basketId,
            ["shippingAddress"] = new Address { Street1 = "123 Main St", City = "Anytown", State = "CA", PostCode = "12345" }
        }).ConfigureAwait(false);
        basketWithAddress.GetProperty("shippingAddress").GetProperty("street1").GetString().Should().Be("123 Main St");

        var checkedOutBasket = await CallToolAsync(client, "checkout_basket", new Dictionary<string, object?> { ["basketId"] = basketId }).ConfigureAwait(false);
        checkedOutBasket.GetProperty("status").GetString().Should().Be("C");

        Test.Checkpoint("Confirm inventory is asynchronously confirmed by the real Products subscriber.");
        var movements = GetMovements(basketId);
        for (var attempt = 1; movements.All(x => x.StatusCode != MovementStatus.Confirmed); attempt++)
        {
            if (attempt > 60)
                Assert.Fail($"Inventory movement for MCP Basket '{basketId}' was not confirmed after 60 polls.");

            Test.Delay(1000, $"Waiting for the MCP Basket's inventory movement to be confirmed; iteration {attempt} of 60.");
            movements = GetMovements(basketId);
        }
    }

    private static async Task<JsonElement> CallToolAsync(McpClient client, string name, Dictionary<string, object?> arguments)
    {
        var result = await client.CallToolAsync(name, arguments).ConfigureAwait(false);
        if (result.IsError == true)
            Assert.Fail($"MCP tool '{name}' failed: {result.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text}");

        return result.StructuredContent ?? throw new InvalidOperationException($"MCP tool '{name}' returned no structured content.");
    }

    private Movement[] GetMovements(string basketId) => Test.Http<Movement[]>(_productsApi)
        .Run(HttpMethod.Get, "/api/inventory/movements", r => r.WithQuery($"referenceid eq '{basketId}'"))
        .AssertOK()
        .Value!;
}
