namespace Contoso.Shopping.Infrastructure.Clients;

/// <summary>
/// Provides the HTTP facade for interacting with the external Products API.
/// </summary>
/// <param name="httpClient">The <see cref="HttpClient"/>.</param>
public class ProductsHttpClient(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient.ThrowIfNull();

    /// <summary>
    /// Creates a new inventory reservation.
    /// </summary>
    /// <param name="request">The <see cref="MovementRequest"/>.</param>
    public async Task<Result> CreateReservationAsync(MovementRequest request, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/inventory/reserve", request, JsonDefaults.SerializerOptions, ct).ConfigureAwait(false);
        return await response.ToResultAsync().ConfigureAwait(false);  // Handles the response and returns errors/exceptions as expected.
    }
}