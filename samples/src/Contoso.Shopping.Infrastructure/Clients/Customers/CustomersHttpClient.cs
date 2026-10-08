namespace Contoso.Shopping.Infrastructure.Clients.Customers;

/// <summary>
/// Provides the HTTP facade for interacting with the external Customers API.
/// </summary>
/// <param name="httpClient">The <see cref="HttpClient"/>.</param>
public class CustomersHttpClient(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient.ThrowIfNull();

    /// <summary>
    /// Gets the customer; results in a <see cref="NotFoundException"/> where the customer does not exist.
    /// </summary>
    /// <param name="id">The customer identifier.</param>
    public async Task<Result<Customer>> GetAsync(string id, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"api/customers/{Uri.EscapeDataString(id)}", ct).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return Result.NotFoundError();

        return await response.ToResultAsync<Customer>(ct).ConfigureAwait(false);  // Handles the response and returns errors/exceptions as expected.
    }
}
