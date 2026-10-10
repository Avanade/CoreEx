namespace Contoso.E2E.Runner.Scenarios;

/// <summary>
/// Scenario: Product query lifecycle.
/// </summary>
[Scenario("Customers-Query", "Customer Query Lifecycle", 1)]
public class CustomerQueryScenario : IScenario
{
    /// <inheritdoc/>
    public async Task RunAsync(ScenarioContext context)
    {
        var sb = new StringBuilder();

        var val = Random.Shared.Next(0, 4);
        if (val == 0)
        {
            var customerTypes = await context.StepAsync($"Get all customer types.", async () =>
            {
                var response = await context.TestContext.CustomersHttpClient.GetAsync($"/api/refdata/customer-types");
                return await response.GetValueAsync<CustomerType[]>();
            }, result => "Customer types retrieved successfully.");

            var customerType = customerTypes![Random.Shared.Next(0, customerTypes.Length)];
            sb.Append($"customertype eq '{customerType.Code}'");

            await ScenarioContext.RandomizedDelayAsync(context);
        }

        val = Random.Shared.Next(0, 3);
        if (val == 1 || val == 2)
        {
            if (sb.Length > 0)
                sb.Append(" and ");

            sb.Append($"contains(email, 'example')");
        }

        await context.StepAsync($"Query {sb}", async () =>
        {
            var response = await context.TestContext.CustomersHttpClient.GetAsync($"/api/customers?$filter={sb}");
            var customers = await response.GetValueAsync<Customer[]>();
            return customers;
        }, result => $"Query returned {result!.Length} customers.");
    }
}
