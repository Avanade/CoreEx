namespace Contoso.Shopping.Infrastructure.Clients.Customers;

/// <summary>
/// Provides the HTTP facade for interacting with the external Customers API.
/// </summary>
/// <param name="httpClient">The <see cref="HttpClient"/>.</param>
public class CustomersHttpClient(HttpClient httpClient)
{
    private static readonly Validator<Customer> _validator = Validator.Create<Customer>()
        .HasProperty(x => x.Email, c => c.Mandatory().Email());

    private readonly HttpClient _httpClient = httpClient.ThrowIfNull();

    /// <summary>
    /// Gets the customer, or returns a not-found result when the customer does not exist.
    /// </summary>
    /// <param name="id">The customer identifier.</param>
    public async Task<Result<Customer>> GetAsync(string id, CancellationToken ct = default)
    {
        using var response = await _httpClient.GetAsync($"api/customers/{Uri.EscapeDataString(id)}", ct).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return Result.NotFoundError();

        return await response.WithValidator(_validator).ToResultAsync(ct).ConfigureAwait(false);
    }
}
