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
    public Task<Result<Customer>> GetAsync(string id, CancellationToken ct = default)
        => _httpClient
            .Send(HttpMethod.Get, $"api/customers/{Uri.EscapeDataString(id)}")
            .WithStatusResult(HttpStatusCode.NotFound, () => Result.NotFoundError())
            .WithValidator(_validator)
            .ToResultAsync(ct);
}
