namespace Contoso.Shopping.Test.Subscribe;

public partial class SubscriberTests
{
    [Test]
    public void BasketCheckedOut_SendsConfirmationEmail() => Test.Scoped(test =>
    {
        // Arrange - simulate a basket that has just been checked out.
        var basket = new Basket
        {
            Id = "basket-1",
            CustomerId = 16.ToGuid().ToString(),
            Pricing = new BasketPricing { SubTotal = 100m, Total = 100m }
        };

        var ed = new EventData().WithTitle("contoso.shopping.basket.checkedout.v1").WithValue(basket);
        var ce = Test.CreateCloudEventFrom(ed);
        var sbm = ce.ToServiceBusReceivedMessage();

        // The customer's email is resolved in real-time from the Customers API (golden value Frank Foster, ^16).
        _mockHttpGetCustomerRequest.Respond.WithJson(new { id = 16.ToGuid().ToString(), firstName = "Frank", lastName = "Foster", email = "frank.foster@example.com" });

        _mockHttpSendMailRequest.WithAnyBody()
            .Respond.With(HttpStatusCode.Accepted);

        // Act - receive the event which should trigger the confirmation email send.
        var r = test.Run(async _ =>
        {
            var sbs = test.Services.GetRequiredService<ServiceBusSubscribedSubscriber>();
            return await sbs.ReceiveAsync(sbm);
        }).AssertSuccess();

        r.Value.IsSuccess.Should().BeTrue();

        // Assert - the Customers API and SendGrid API were invoked to send the confirmation email.
        _mockHttpGetCustomerRequest.Verify();
        _mockHttpSendMailRequest.Verify();
    });
}
