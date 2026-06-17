namespace Contoso.Shopping.Test.Subscribe;

public partial class SubscriberTests 
{
    [Test]
    public void ProductModify_Found() => Test.Scoped(async test =>
    {
        // Arrange - ensure the product exists and prepare a message to modify it.
        var pa = test.Services.GetRequiredService<IProductAdapter>();
        var pr = await pa.GetAsync(22.ToGuid().ToString());
        pr.IsSuccess.Should().BeTrue();
        pr.Value.Should().NotBeNull();
        var p = pr.Value;
        p.Text += " modified";

        var ed = new EventData().WithTitle("contoso.products.product.updated.v1").WithValue(p);
        var ce = Test.CreateCloudEventFrom(ed);
        var sbm = ce.ToServiceBusReceivedMessage();

        // Act - modify the product.
        var r = test.Run(async _ =>
        {
            var sbs = test.Services.GetRequiredService<ServiceBusSubscribedSubscriber>();
            return await sbs.ReceiveAsync(sbm);
        }).AssertSuccess();

        r.Value.IsSuccess.Should().BeTrue();

        // Assert - the product should be updated.
        pr = await pa.GetAsync(22.ToGuid().ToString());
        pr.IsSuccess.Should().BeTrue();
        pr.Value.Should().NotBeNull();
        pr.Value.Text.Should().EndWith(" modified");
    });

    [Test]
    public void ProductModify_NotFound() => Test.Scoped(async test =>
    {
        // Arrange - get a product and clone it.
        var pa = test.Services.GetRequiredService<IProductAdapter>();
        var pr = await pa.GetAsync(24.ToGuid().ToString());
        pr.IsSuccess.Should().BeTrue();
        pr.Value.Should().NotBeNull();
        var p = pr.Value;

        p.Id = Runtime.NewId();
        p.Sku += "-NEW";
        p.Text += " new";

        var ed = new EventData().WithTitle("contoso.products.product.created.v1").WithValue(p);
        var ce = Test.CreateCloudEventFrom(ed);
        var sbm = ce.ToServiceBusReceivedMessage();

        // Act - modify the product.
        var r = test.Run(async _ =>
        {
            var sbs = test.Services.GetRequiredService<ServiceBusSubscribedSubscriber>();
            return await sbs.ReceiveAsync(sbm);
        }).AssertSuccess();

        r.Value.IsSuccess.Should().BeTrue();

        // Assert - the product should be updated.
        pr = await pa.GetAsync(p.Id);
        pr.IsSuccess.Should().BeTrue();
        pr.Value.Should().NotBeNull();
        pr.Value.Text.Should().EndWith(" new");
    });
}