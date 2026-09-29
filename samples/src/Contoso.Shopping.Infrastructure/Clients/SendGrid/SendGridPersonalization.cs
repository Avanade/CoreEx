namespace Contoso.Shopping.Infrastructure.Clients.SendGrid;

/// <summary>
/// Represents a SendGrid v3 <c>personalization object</c> (see <see href="https://www.twilio.com/docs/sendgrid/api-reference/mail-send/mail-send"/>).
/// </summary>
public class SendGridPersonalization
{
    /// <summary>
    /// Gets or sets the recipient(s) of the email.
    /// </summary>
    public List<SendGridEmailAddress>? To { get; set; }

    /// <summary>
    /// Gets or sets the subject of the email.
    /// </summary>
    public string? Subject { get; set; }
}
