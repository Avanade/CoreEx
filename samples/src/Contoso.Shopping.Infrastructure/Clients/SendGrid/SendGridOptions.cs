namespace Contoso.Shopping.Infrastructure.Clients.SendGrid;

/// <summary>
/// Represents the configuration options for sending mail via the SendGrid API.
/// </summary>
public class SendGridOptions
{
    /// <summary>
    /// Gets or sets the sending API key.
    /// </summary>
    /// <remarks>Not used directly by <see cref="SendGridHttpClient"/>; applied as the <c>Authorization</c> header when the underlying <see cref="System.Net.Http.HttpClient"/> is registered.</remarks>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the "from" email address used for all outgoing mail.
    /// </summary>
    public string? FromEmail { get; set; }

    /// <summary>
    /// Gets or sets the "from" display name used for all outgoing mail.
    /// </summary>
    public string? FromName { get; set; }
}
