namespace Contoso.Shopping.Infrastructure.Adapters.Notifications;

[ScopedService<INotificationAdapter>]
public class NotificationAdapter(SendGridHttpClient client, IOptions<SendGridOptions> options) : INotificationAdapter
{
    private readonly SendGridHttpClient _client = client.ThrowIfNull();
    private readonly SendGridOptions _options = options.ThrowIfNull().Value;

    /// <inheritdoc/>
    /// <remarks>The basket has no email address of its own (only a <see cref="Contracts.Basket.CustomerId"/>); as Shopping has no adapter into the Customers domain, a placeholder recipient
    /// address is synthesized from the customer identifier. In a real solution this would instead be resolved via a Customers domain adapter.</remarks>
    public Task<Result> SendBasketCheckedOutConfirmationAsync(Contracts.Basket basket, CancellationToken ct = default)
    {
        var request = new SendGridMailRequest
        {
            From = new SendGridEmailAddress { Email = _options.FromEmail, Name = _options.FromName },
            Personalizations =
            [
                new SendGridPersonalization
                {
                    To = [new SendGridEmailAddress { Email = $"{basket.CustomerId}@customer.contoso.local" }],
                    Subject = $"Your Contoso order {basket.Id} is confirmed"
                }
            ],
            Content =
            [
                new SendGridContent
                {
                    Type = "text/plain",
                    Value = $"Hi, thanks for shopping with Contoso! Your basket {basket.Id} has been checked out and is now confirmed. Order total: {basket.Pricing?.Total:C}."
                }
            ]
        };

        return _client.SendMailAsync(request, ct);
    }
}
