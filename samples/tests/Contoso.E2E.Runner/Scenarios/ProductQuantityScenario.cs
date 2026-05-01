using Azure;

namespace Contoso.E2E.Runner.Scenarios;

[Scenario("Products-Quantity", "Product Quantity Lifecycle", 2)]
public class ProductQuantityScenario : IScenario
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

        // Step 2: Select a random product and get the quantity.
        var product = _products![Random.Shared.Next(0, _products.Length - 1)];
        await context.StepAsync("Get product quantity.", async () =>
        {
            var response = await context.TestContext.ProductsHttpClient.GetAsync($"/api/products/{product.Id}/on-hand");
            return await response.GetValueAsync<decimal>();
        }, q => $"Product '{product.Sku}' has quantity: {q}.");
    }
}