namespace Contoso.Shopping.Infrastructure.Clients.SendGrid;

/// <summary>
/// Represents a SendGrid v3 <c>email object</c> (see <see href="https://www.twilio.com/docs/sendgrid/api-reference/mail-send/mail-send"/>).
/// </summary>
public class SendGridEmailAddress
{
    /// <summary>
    /// Gets or sets the email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string? Name { get; set; }
}
