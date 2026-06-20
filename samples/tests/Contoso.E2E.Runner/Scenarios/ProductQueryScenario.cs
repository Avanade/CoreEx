namespace Contoso.E2E.Runner.Scenarios;

/// <summary>
/// Scenario: Product query lifecycle.
/// </summary>
[Scenario("Products-Query", "Product Query Lifecycle", 1)]
public class ProductQueryScenario : IScenario
{
    /// <inheritdoc/>
    public async Task RunAsync(ScenarioContext context)
    {
        var sb = new StringBuilder();

        var val = Random.Shared.Next(0, 3);
        if (val == 0)
        {
            var categories = await context.StepAsync($"Get all categories.", async () =>
            {
                var response = await context.TestContext.ProductsHttpClient.GetAsync($"/api/refdata/categories");
                return await response.GetValueAsync<Category[]>();
            }, result => "Categories retrieved successfully.");

            var category = categories![Random.Shared.Next(0, categories.Length)];
            sb.Append($"category eq '{category.Code}'");

            await ScenarioContext.RandomizedDelayAsync(context);
        }

        val = Random.Shared.Next(0, 3);
        if (val == 0)
        {
            var brands = await context.StepAsync($"Get all brands.", async () =>
            {
                var response = await context.TestContext.ProductsHttpClient.GetAsync($"/api/refdata/brands");
                return await response.GetValueAsync<Brand[]>();
            }, result => "Brands retrieved successfully.");

            var brand = brands![Random.Shared.Next(0, brands.Length)];
            if (sb.Length > 0)
                sb.Append(" and ");

            sb.Append($"brand eq '{brand.Code}'");

            await ScenarioContext.RandomizedDelayAsync(context);
        }

        val = Random.Shared.Next(0, 3);
        if (val == 1 || val == 2)
        {
            if (sb.Length > 0)
                sb.Append(" and ");

            if (val == 1)
                sb.Append($"startswith(sku, 'Y')");
            else
                sb.Append($"endswith(text, 's')");
        }

        await context.StepAsync($"Query {sb}", async () =>
        {
            var response = await context.TestContext.ProductsHttpClient.GetAsync($"/api/products?$filter={sb}");
            var products = await response.GetValueAsync<Product[]>();
            return products;
        }, result => $"Query returned {result!.Length} products.");
    }
}