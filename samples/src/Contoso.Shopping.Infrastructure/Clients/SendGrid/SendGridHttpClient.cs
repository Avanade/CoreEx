namespace Contoso.Shopping.Infrastructure.Clients.SendGrid;

/// <summary>
/// Provides the HTTP facade for interacting with the external SendGrid v3 Mail Send API (see <see href="https://www.twilio.com/docs/sendgrid/api-reference/mail-send/mail-send"/>).
/// </summary>
/// <param name="httpClient">The <see cref="HttpClient"/>.</param>
public class SendGridHttpClient(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient.ThrowIfNull();

    /// <summary>
    /// Sends the specified mail <paramref name="request"/>.
    /// </summary>
    /// <param name="request">The <see cref="SendGridMailRequest"/>.</param>
    /// <remarks>A successful send returns <see cref="System.Net.HttpStatusCode.Accepted"/> (202) with no response body.</remarks>
    public async Task<Result> SendMailAsync(SendGridMailRequest request, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync("v3/mail/send", request, JsonDefaults.SerializerOptions, ct).ConfigureAwait(false);
        return await response.ToResultAsync(ct).ConfigureAwait(false);  // Handles the response and returns errors/exceptions as expected.
    }
}
