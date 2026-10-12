namespace Contoso.E2E.Runner.Scenarios;

/// <summary>
/// Scenario: Customer update lifecycle.
/// </summary>
[Scenario("Customers-Update", "Customer Update Lifecycle", 1)]
public class CustomerUpdateScenario : IScenario
{
    private static readonly SemaphoreSlim _semaphore = new(1);
    private CustomerLite[]? _customers;

    /// <summary>
    /// Gets all the customers (limited to 100) for use in the scenario.
    /// </summary>
    public static async Task<CustomerLite[]> GetAllCustomersAsync(ScenarioContext context)
    {
        var response = await context.TestContext.CustomersHttpClient.GetAsync("/api/customers?$take=100");
        var customers = await response.GetValueAsync<CustomerLite[]>();
        return customers ?? [];
    }

    /// <inheritdoc/>
    public async Task RunAsync(ScenarioContext context)
    {
        // Step 1: Find all the customers (first time only).
        _semaphore.Wait();
        try
        {
            if (_customers is null)
            {
                _customers = await context.StepAsync("Find all customers.", async () =>
                {
                    return await GetAllCustomersAsync(context);
                }, result => $"{result!.Length} customer(s) found.");

                await ScenarioContext.RandomizedDelayAsync(context);
            }
        }
        finally
        {
            _semaphore.Release();
        }

        // Step 2: Select a random customer and full get.
        var index = Random.Shared.Next(0, _customers!.Length - 1);
        var c = _customers[index];

        var customer = await context.StepAsync($"Get: '{c.Id}'.", async () =>
        {
            var response = await context.TestContext.CustomersHttpClient.GetAsync($"/api/customers/{c.Id}");
            return await response.GetValueAsync<Customer>();
        }, result => "Customer retrieved successfully.");

        await ScenarioContext.RandomizedDelayAsync(context);

        // Step 3: Update customer details.
        if (customer!.LastName!.EndsWith(" (updated)"))
            customer.LastName = customer.LastName[..^10];
        else
            customer.LastName += " (updated)";

        await context.StepAsync($"Update: '{customer.Id}'.", async () =>
        {
            var response = await context.TestContext.CustomersHttpClient.PutAsJsonAsync($"/api/customers/{customer.Id}", customer, JsonDefaults.SerializerOptions);
            return await response.GetValueAsync<Customer>();
        }, c => $"Customer {c!.Id} updated.");
    }
}
