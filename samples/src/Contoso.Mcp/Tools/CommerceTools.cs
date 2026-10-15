namespace Contoso.Mcp.Tools;

[McpServerToolType]
public static class CommerceTools
{
    /// <summary>Searches products using the Products server's configured ranked retrieval mode.</summary>
    [McpServerTool(UseStructuredContent = true, ReadOnly = true), Description("Find active products using the Products server's ranked search mode (semantic by default; optionally English full-text). Returns up to the requested number of summaries, most relevant first. Results are candidates, not guaranteed matches; full-text can return none. Requires a prepared index.")]
    public static Task<ProductLite[]> SearchProductsAsync(
        [FromServices] IHttpClientFactory clientFactory,
        [Description("Product text, intended use, or a SKU. Semantic mode does not guarantee exact SKU matching; FullText mode prioritizes an exact eligible SKU and otherwise requires lexical matches.")] string searchTerm,
        [Description("Maximum number of products to return (1-25).")] int take = 10,
        CancellationToken cancellationToken = default)
    {
        searchTerm = searchTerm.Required();
        if (take is < 1 or > 25)
            throw new ArgumentOutOfRangeException(nameof(take), "The result limit must be between 1 and 25.");

        return ProductsApi(clientFactory)
            .Send(HttpMethod.Get, "/api/products")
            .WithSearch(searchTerm)
            .WithPaging(take: take)
            .GetValueAsync<ProductLite[]>(cancellationToken);
    }

    /// <summary>Gets the full details for a product.</summary>
    [McpServerTool(UseStructuredContent = true, ReadOnly = true), Description("Get product details by product ID.")]
    public static Task<Product> GetProductAsync(
        [FromServices] IHttpClientFactory clientFactory,
        [Description("The product ID.")] string productId,
        CancellationToken cancellationToken = default)
        => ProductsApi(clientFactory)
            .Send(HttpMethod.Get, $"/api/products/{Uri.EscapeDataString(productId.Required())}")
            .GetValueAsync<Product>(cancellationToken);

    /// <summary>Gets a basket by its ID.</summary>
    [McpServerTool(UseStructuredContent = true, ReadOnly = true), Description("Get basket status, items, pricing, and shipping address by basket ID.")]
    public static Task<Basket> GetBasketAsync(
        [FromServices] IHttpClientFactory clientFactory,
        [Description("The basket ID.")] string basketId,
        CancellationToken cancellationToken = default)
        => ShoppingApi(clientFactory)
            .Send(HttpMethod.Get, $"/api/baskets/{Uri.EscapeDataString(basketId.Required())}")
            .GetValueAsync<Basket>(cancellationToken);

    /// <summary>Lists baskets owned by a customer.</summary>
    [McpServerTool(UseStructuredContent = true, ReadOnly = true), Description("List a customer's baskets. Requires a customer ID that exists in the Customers domain.")]
    public static Task<Basket[]> ListCustomerBasketsAsync(
        [FromServices] IHttpClientFactory clientFactory,
        [Description("The existing customer ID. The sample E2E setup seeds customer 16.ToGuid() with an address.")] string customerId,
        [Description("Maximum number of baskets to return (1-25).")] int take = 10,
        CancellationToken cancellationToken = default)
    {
        if (take is < 1 or > 25)
            throw new ArgumentOutOfRangeException(nameof(take), "The result limit must be between 1 and 25.");

        return ShoppingApi(clientFactory)
            .Send(HttpMethod.Get, $"/api/customers/{Uri.EscapeDataString(customerId.Required())}/baskets")
            .WithPaging(take: take)
            .GetValueAsync<Basket[]>(cancellationToken);
    }

    /// <summary>Creates a basket for an existing customer.</summary>
    [McpServerTool(UseStructuredContent = true), Description("Create a new basket for an existing customer. The customer's shipping address is copied when available.")]
    public static Task<Basket> CreateBasketAsync(
        [FromServices] IHttpClientFactory clientFactory,
        [Description("The ID of an existing customer.")] string customerId,
        CancellationToken cancellationToken = default)
        => ShoppingApi(clientFactory)
            .Send(HttpMethod.Post, $"/api/customers/{Uri.EscapeDataString(customerId.Required())}/baskets")
            .GetValueAsync<Basket>(cancellationToken);

    /// <summary>Adds a product to a basket.</summary>
    [McpServerTool(UseStructuredContent = true), Description("Add a product and quantity to an active basket.")]
    public static Task<Basket> AddBasketItemAsync(
        [FromServices] IHttpClientFactory clientFactory,
        [Description("The basket ID.")] string basketId,
        [Description("The product ID.")] string productId,
        [Description("The quantity to add.")] decimal quantity,
        CancellationToken cancellationToken = default)
        => ShoppingApi(clientFactory)
            .SendValue(
                HttpMethod.Post,
                $"/api/baskets/{Uri.EscapeDataString(basketId.Required())}/items",
                new BasketItemAddRequest { ProductId = productId.Required(), Quantity = quantity })
            .GetValueAsync<Basket>(cancellationToken);

    /// <summary>Changes the quantity of a basket item.</summary>
    [McpServerTool(UseStructuredContent = true), Description("Set an item's quantity in an active basket. The current item ETag is fetched and sent to the API for concurrency validation.")]
    public static async Task<Basket> UpdateBasketItemAsync(
        [FromServices] IHttpClientFactory clientFactory,
        [Description("The basket ID.")] string basketId,
        [Description("The basket item ID.")] string basketItemId,
        [Description("The new quantity.")] decimal quantity,
        CancellationToken cancellationToken = default)
    {
        var client = ShoppingApi(clientFactory);
        var basket = await client
            .Send(HttpMethod.Get, $"/api/baskets/{Uri.EscapeDataString(basketId.Required())}")
            .GetValueAsync<Basket>(cancellationToken).ConfigureAwait(false);

        var item = basket.Items?.SingleOrDefault(x => x.Id == basketItemId.Required())
            ?? throw new InvalidOperationException($"Basket item '{basketItemId}' was not found in basket '{basketId}'.");

        return await client
            .SendValue(
                HttpMethod.Put,
                $"/api/baskets/{Uri.EscapeDataString(basketId)}/items/{Uri.EscapeDataString(basketItemId)}",
                new BasketItemUpdateRequest { Quantity = quantity, ETag = item.ETag })
            .GetValueAsync<Basket>(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Removes an item from a basket.</summary>
    [McpServerTool(UseStructuredContent = true, Destructive = true), Description("Remove a product from an active basket by basket item ID.")]
    public static Task<Basket> RemoveBasketItemAsync(
        [FromServices] IHttpClientFactory clientFactory,
        [Description("The basket ID.")] string basketId,
        [Description("The basket item ID.")] string basketItemId,
        CancellationToken cancellationToken = default)
        => ShoppingApi(clientFactory)
            .Send(HttpMethod.Delete, $"/api/baskets/{Uri.EscapeDataString(basketId.Required())}/items/{Uri.EscapeDataString(basketItemId.Required())}")
            .GetValueAsync<Basket>(cancellationToken);

    /// <summary>Applies a discount coupon to a basket.</summary>
    [McpServerTool(UseStructuredContent = true), Description("Apply a coupon code to an active basket.")]
    public static Task<Basket> ApplyBasketDiscountAsync(
        [FromServices] IHttpClientFactory clientFactory,
        [Description("The basket ID.")] string basketId,
        [Description("The discount coupon code.")] string couponCode,
        CancellationToken cancellationToken = default)
        => ShoppingApi(clientFactory)
            .Send(HttpMethod.Put, $"/api/baskets/{Uri.EscapeDataString(basketId.Required())}/apply-discount/{Uri.EscapeDataString(couponCode.Required())}")
            .GetValueAsync<Basket>(cancellationToken);

    /// <summary>Changes the shipping address for a basket.</summary>
    [McpServerTool(UseStructuredContent = true), Description("Set or replace the shipping address for an active basket.")]
    public static Task<Basket> UpdateBasketShippingAddressAsync(
        [FromServices] IHttpClientFactory clientFactory,
        [Description("The basket ID.")] string basketId,
        Address shippingAddress,
        CancellationToken cancellationToken = default)
        => ShoppingApi(clientFactory)
            .SendValue(HttpMethod.Put, $"/api/baskets/{Uri.EscapeDataString(basketId.Required())}/shipping-address", shippingAddress)
            .GetValueAsync<Basket>(cancellationToken);

    /// <summary>Checks out a basket and starts inventory reservation.</summary>
    [McpServerTool(UseStructuredContent = true, Destructive = true), Description("Check out the specified active basket. This is a consequential action: it reserves inventory and starts asynchronous confirmation through the outbox and Service Bus.")]
    public static Task<Basket> CheckoutBasketAsync(
        [FromServices] IHttpClientFactory clientFactory,
        [Description("The ID of the basket to check out.")] string basketId,
        CancellationToken cancellationToken = default)
        => ShoppingApi(clientFactory)
            .Send(HttpMethod.Post, $"/api/baskets/{Uri.EscapeDataString(basketId.Required())}/checkout")
            .GetValueAsync<Basket>(cancellationToken);

    /// <summary>Creates the named client for the Products API.</summary>
    private static HttpClient ProductsApi(IHttpClientFactory clientFactory) => clientFactory.CreateClient("ProductsApi");

    /// <summary>Creates the named client for the Shopping API.</summary>
    private static HttpClient ShoppingApi(IHttpClientFactory clientFactory) => clientFactory.CreateClient("ShoppingApi");
}
