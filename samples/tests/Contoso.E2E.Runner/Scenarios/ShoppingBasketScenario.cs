namespace Contoso.E2E.Runner.Scenarios;

/// <summary>
/// Scenario: Shopping basket lifecycle.
/// </summary>
[Scenario("Shopping-Basket", "Shopping Basket Lifecycle", 2)]
public class ShoppingBasketScenario : IScenario
{
    private static readonly SemaphoreSlim _semaphore = new(1);
    private ProductLite[]? _products;

    /// <inheritdoc/>
    public async Task RunAsync(ScenarioContext context)
    {
        // Step 1: Find all the products (first time only).
        _semaphore.Wait();
        try
        {
            if (_products is null)
            {
                _products = await context.StepAsync("Find all products.", async () =>
                {
                    return await ProductUpdateScenario.GetAllProductsAsync(context);
                }, result => $"{result!.Length} product(s) found.");

                await ScenarioContext.RandomizedDelayAsync(context);
            }
        }
        finally
        {
            _semaphore.Release();
        }

        // Step 2: Create a new basket
        var basket = await context.StepAsync("Create new basket.", async () =>
        {
            var response = await context.TestContext.ShoppingHttpClient.PostAsync($"/api/customers/test/baskets", null);
            return await response.GetValueAsync<Basket>();
        }, b => $"Basket '{b!.Id}' created.");

        await ScenarioContext.RandomizedDelayAsync(context);

        // Step 3: Add items to the basket
        for (int i = 0; i < Random.Shared.Next(1, 5); i++)
        {
            var product = _products![Random.Shared.Next(0, _products.Length - 1)];
            var quantity = Random.Shared.Next(1, 3);

            basket = await context.StepAsync($"Add {quantity} x {product.Sku}.", async () =>
            {
                var item = new BasketItemAddRequest
                {
                    ProductId = product.Id,
                    Quantity = quantity
                };

                var response = await context.TestContext.ShoppingHttpClient.PostAsJsonAsync($"/api/baskets/{basket!.Id}/items", item, JsonDefaults.SerializerOptions);
                return await response.GetValueAsync<Basket>();
            }, _ => "Item added.");

            await ScenarioContext.RandomizedDelayAsync(context);
        }

        // Step 4: Apply discount coupon (10% off)
        if (Random.Shared.Next(1, 3) == 2)
        {
            basket = await context.StepAsync("Apply discount coupon 'SAVE10'.", async () =>
            {
                var response = await context.TestContext.ShoppingHttpClient.PutAsync($"/api/baskets/{basket!.Id}/apply-discount/SAVE10", null);
                return await response.GetValueAsync<Basket>();
            }, b => $"Discount applied.");
            await ScenarioContext.RandomizedDelayAsync(context);
        }

        await ScenarioContext.RandomizedDelayAsync(context);

        // Step 5: Checkout the basket
        basket = await context.StepAsync("Checkout basket.", async () =>
        {
            var response = await context.TestContext.ShoppingHttpClient.PostAsync($"/api/baskets/{basket!.Id}/checkout", null);
            return await response.GetValueAsync<Basket>();
        }, b => $"Basket checked-out.");

        await ScenarioContext.RandomizedDelayAsync(context);

        // Step 6: Get the basket.
        basket = await context.StepAsync("Get checked-out basket.", async () =>
        {
            var response = await context.TestContext.ShoppingHttpClient.GetAsync($"/api/baskets/{basket!.Id}");
            return await response.GetValueAsync<Basket>() ?? throw new NotFoundException();
        }, b => $"Basket retrieved.");
    }
}