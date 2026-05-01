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
    public async Task<Result> CreateReservationAsync(MovementRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/inventory/reserve", request, JsonDefaults.SerializerOptions);
        return await response.ToResultAsync();  // Handles the response and returns errors/exceptions as expected.
    }
}