namespace Contoso.Shopping.Infrastructure.Adapters.Notifications;

[ScopedService<INotificationAdapter>]
public class NotificationAdapter(SendGridHttpClient client, ICustomerAdapter customerAdapter, IOptions<SendGridOptions> options) : INotificationAdapter
{
    private readonly SendGridHttpClient _client = client.ThrowIfNull();
    private readonly ICustomerAdapter _customerAdapter = customerAdapter.ThrowIfNull();
    private readonly SendGridOptions _options = options.ThrowIfNull().Value;

    /// <inheritdoc/>
    /// <remarks>The basket has no email address of its own (only a <see cref="Contracts.Basket.CustomerId"/>); the recipient is therefore resolved in real-time via the <see cref="ICustomerAdapter"/>.</remarks>
    public async Task<Result> SendBasketCheckedOutConfirmationAsync(Contracts.Basket basket, CancellationToken ct = default)
    {
        var cr = await _customerAdapter.GetAsync(basket.CustomerId!, ct).ConfigureAwait(false);
        if (cr.IsFailure)
            return cr.AsResult();

        var customer = cr.Value;
        var request = new SendGridMailRequest
        {
            From = new SendGridEmailAddress { Email = _options.FromEmail, Name = _options.FromName },
            Personalizations =
            [
                new SendGridPersonalization
                {
                    To = [new SendGridEmailAddress { Email = customer.Email!, Name = $"{customer.FirstName} {customer.LastName}".Trim() }],
                    Subject = $"Your Contoso order {basket.Id} is confirmed"
                }
            ],
            Content =
            [
                new SendGridContent
                {
                    Type = "text/plain",
                    Value = $"Hi {customer.FirstName}, thanks for shopping with Contoso! Your basket {basket.Id} has been checked out and is now confirmed. Order total: {basket.Pricing?.Total:C}."
                }
            ]
        };

        return await _client.SendMailAsync(request, ct).ConfigureAwait(false);
    }
}
