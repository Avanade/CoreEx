namespace Contoso.Shopping.Infrastructure.Clients.SendGrid;

/// <summary>
/// Represents the SendGrid v3 <c>POST /v3/mail/send</c> request body (see <see href="https://www.twilio.com/docs/sendgrid/api-reference/mail-send/mail-send"/>).
/// </summary>
public class SendGridMailRequest
{
    /// <summary>
    /// Gets or sets the personalization(s) - i.e. the recipient(s) and per-recipient subject.
    /// </summary>
    public List<SendGridPersonalization>? Personalizations { get; set; }

    /// <summary>
    /// Gets or sets the sender.
    /// </summary>
    public SendGridEmailAddress? From { get; set; }

    /// <summary>
    /// Gets or sets the content(s) of the email.
    /// </summary>
    public List<SendGridContent>? Content { get; set; }
}
