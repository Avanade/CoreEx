namespace Contoso.E2E.Runner.Scenarios;

/// <summary>
/// Provides data seeding for the E2E testing.
/// </summary>
[ScenarioSetUp("Data-Seeding", "Data Seeding for E2E Testing", 2)]
public class DataSeedingSetup : IScenario
{
    /// <inheritdoc/>
    public async Task RunAsync(ScenarioContext context)
    {
        // Step 1: Find all the products.
        var products = await context.StepAsync("Find all products.", async () =>
        {
            return await ProductUpdateScenario.GetAllProductsAsync(context);
        }, result => $"{result!.Length} product(s) found");

        // Step 2: Adjust inventory for each product to ensure they have plenty in stock.
        await context.StepAsync("Adjust inventory for all active and stocked products.", async () =>
        {
            var req = new MovementRequest
            {
                Id = Runtime.NewId(),
                Products = products!.Where(x => !(x.IsInactive || x.IsNonStocked)).ToDataMap(
                    p => p.Id!,
                    p => new MovementRequestProduct
                    {
                        Quantity = 1000,
                        UnitOfMeasureCode = p.UnitOfMeasureCode
                    })
            };

            var response = await context.TestContext.ProductsHttpClient.PostAsJsonAsync("/api/inventory/adjust", req, JsonDefaults.SerializerOptions);
            return await response.GetValueAsync<Movement[]>();
        }, result => $"{result!.Length} inventory adjustment(s).");
    }
}