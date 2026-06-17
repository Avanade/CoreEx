namespace Contoso.Products.Test.Subscribe;

public partial class SubscriberTests 
{
    [Test]
    public void ReservationCancel_NotFound() => Test.Scoped(test =>
    {
        var ed = EventData.CreateCommand("products", "reservation", "cancel").WithKey("abc");
        var ce = Test.CreateCloudEventFrom(ed);
        var sbm = ce.ToServiceBusReceivedMessage();

        test.Run(async _ =>
        {
            var sbs = test.Services.GetRequiredService<ServiceBusSubscribedSubscriber>();
            var r = await sbs.ReceiveAsync(sbm);

            r.IsFailure.Should().BeTrue();
            var e = r.Error.Should().BeOfType<EventSubscriberHandledException>().Subject;
            e.ErrorHandling.Should().Be(ErrorHandling.CompleteAsInformation);
            e.InnerException.Should().BeOfType<NotFoundException>().Which.ErrorCode.Should().Be("pending-reservation-not-found");
        }).AssertSuccess();
    });

    [Test]
    public void ReservationCancel_Success() => Test.Scoped(async test =>
    {
        // Arrange - ensure there are reservations to cancel.
        var referenceId = 1001.ToGuid().ToString();

        var ms = test.Services.GetRequiredService<IMovementReadService>();
        var mr = await ms.GetAsync(referenceId);
        mr.Should().NotBeNull().And.HaveCount(2).And.AllSatisfy(m => m.StatusCode.Should().Be(MovementStatus.Pending));

        var qs = test.Services.GetRequiredService<IInventoryService>();
        var q1 = await qs.GetOnHandAsync(mr[0].ProductId!);
        var q2 = await qs.GetOnHandAsync(mr[1].ProductId!);

        // Act - cancel the reservation using a simulated command-based message.
        test.ExpectPostgresOutboxEvents(e => e.AssertCount(2))
            .Run(async _ =>
            {
                var ed = EventData.CreateCommand("products", "reservation", "cancel").WithKey(referenceId);
                var ce = Test.CreateCloudEventFrom(ed);
                var sbm = ce.ToServiceBusReceivedMessage();

                var sbs = test.Services.GetRequiredService<ServiceBusSubscribedSubscriber>();
                var r = await sbs.ReceiveAsync(sbm);
                r.IsSuccess.Should().BeTrue();
            }).AssertSuccess();

        // Assert - the reservation should now be cancelled.
        mr = await ms.GetAsync(referenceId);
        mr.Should().NotBeNull().And.HaveCount(2).And.AllSatisfy(m => m.StatusCode.Should().Be(MovementStatus.Canceled));

        // Assert - the on-hand quantity should be increased by the cancelled reservation quantity.
        var q1After = await qs.GetOnHandAsync(mr[0].ProductId!);
        var q2After = await qs.GetOnHandAsync(mr[1].ProductId!);
        q1After.Should().Be(q1 + mr[0].Quantity * -1);
        q2After.Should().Be(q2 + mr[1].Quantity * -1);
    });
}