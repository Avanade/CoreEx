namespace Contoso.E2E.Runner.Scenarios;

/// <summary>
/// Scenario: Product update lifecycle.
/// </summary>
[Scenario("Products-Update", "Product Update Lifecycle", 1)]
public class ProductUpdateScenario : IScenario
{
    private static readonly SemaphoreSlim _semaphore = new(1);
    private ProductLite[]? _products;

    /// <summary>
    /// Gets all the products (limited to 100) for use in the scenario.
    /// </summary>
    public static async Task<ProductLite[]> GetAllProductsAsync(ScenarioContext context)
    {
        var response = await context.TestContext.ProductsHttpClient.GetAsync("/api/products?$take=100");
        var products = await response.GetValueAsync<ProductLite[]>();
        return products ?? [];
    }

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
                    return await GetAllProductsAsync(context);
                }, result => $"{result!.Length} product(s) found.");

                await ScenarioContext.RandomizedDelayAsync(context);
            }
        }
        finally
        {
            _semaphore.Release();
        }

        // Step 2: Select a random product and full get.
        var index = Random.Shared.Next(0, _products!.Length - 1);
        var p = _products[index];

        var product = await context.StepAsync($"Get: '{p.Id}'.", async () =>
        {
            var response = await context.TestContext.ProductsHttpClient.GetAsync($"/api/products/{p.Id}");
            return await response.GetValueAsync<Product>();
        }, result => "Product retrieved successfully.");

        await ScenarioContext.RandomizedDelayAsync(context);

        // Step 3: Update product description.
        if (product!.Text!.EndsWith(" (updated)"))
            product.Text = product.Text[..^10];
        else
            product.Text += " (updated)";

        await context.StepAsync($"Update: '{product.Id}'.", async () =>
        {
            var response = await context.TestContext.ProductsHttpClient.PutAsJsonAsync($"/api/products/{product.Id}", product, JsonDefaults.SerializerOptions);
            return await response.GetValueAsync<Product>();
        }, p => $"Product {p!.Sku} updated.");
    }
}