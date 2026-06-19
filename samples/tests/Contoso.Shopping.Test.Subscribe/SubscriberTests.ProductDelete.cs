namespace Contoso.Shopping.Test.Subscribe;

public partial class SubscriberTests 
{
    [Test]
    public void ProductDelete_NotFound() => Test.Scoped(test =>
    {
        var ed = new EventData().WithTitle("contoso.products.product.deleted").WithKey(404.ToGuid());
        var ce = Test.CreateCloudEventFrom(ed);
        var sbm = ce.ToServiceBusReceivedMessage();

        test.Run(async _ =>
        {
            var sbs = test.Services.GetRequiredService<ServiceBusSubscribedSubscriber>();
            var r = await sbs.ReceiveAsync(sbm);
            r.IsSuccess.Should().BeTrue();
        }).AssertSuccess();
    });

    [Test]
    public void ProductDelete_Success() => Test.Scoped(async test =>
    {
        // Arrange - ensure there is a product to delete.
        var productId = 20.ToGuid().ToString();

        var pa = test.Services.GetRequiredService<IProductAdapter>();
        var pr = await pa.GetAsync(productId);
        pr.IsSuccess.Should().BeTrue();
        pr.Value.Should().NotBeNull();

        var ed = new EventData().WithTitle("contoso.products.product.deleted").WithKey(productId);
        var ce = Test.CreateCloudEventFrom(ed);
        var sbm = ce.ToServiceBusReceivedMessage();

        // Act - delete the product.
        test.Run(async _ =>
        {
            var sbs = test.Services.GetRequiredService<ServiceBusSubscribedSubscriber>();
            var r = await sbs.ReceiveAsync(sbm);
            r.IsSuccess.Should().BeTrue();
        }).AssertSuccess();

        // Assert - the product should be deleted.
        pr = await pa.GetAsync(productId);
        pr.IsFailure.Should().BeTrue();
        pr.IsNotFoundError.Should().BeTrue();
    });
}