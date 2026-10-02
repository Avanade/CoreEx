namespace Contoso.Shopping.Infrastructure.Clients.SendGrid;

/// <summary>
/// Represents a SendGrid v3 <c>content object</c> (see <see href="https://www.twilio.com/docs/sendgrid/api-reference/mail-send/mail-send"/>).
/// </summary>
public class SendGridContent
{
    /// <summary>
    /// Gets or sets the MIME type of the content (e.g. <c>text/plain</c> or <c>text/html</c>).
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the actual content.
    /// </summary>
    public string? Value { get; set; }
}
