namespace Contoso.Products.Test.Subscribe;

public partial class SubscriberTests 
{
    [Test]
    public void ReservationConfirm_NotFound() => Test.Scoped(test =>
    {
        var ed = EventData.CreateCommand("products", "reservation", "confirm").WithKey("abc");
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
    public void ReservationConfirm_Success() => Test.Scoped(async test =>
    {
        // Arrange - ensure there are reservations to confirm.
        var referenceId = 1000.ToGuid().ToString();

        var ms = test.Services.GetRequiredService<IMovementReadService>();
        var mr = await ms.GetAsync(referenceId);
        mr.Should().NotBeNull().And.HaveCount(3).And.AllSatisfy(m => m.StatusCode.Should().Be(MovementStatus.Pending));

        // Act - confirm the reservation using a simulated command-based message.
        test.ExpectPostgresOutboxEvents(e => e.AssertCount(3))
            .Run(async _ =>
            {
                var ed = EventData.CreateCommand("products", "reservation", "confirm").WithKey(referenceId);
                var ce = Test.CreateCloudEventFrom(ed);
                var sbm = ce.ToServiceBusReceivedMessage();

                var sbs = test.Services.GetRequiredService<ServiceBusSubscribedSubscriber>();
                var r = await sbs.ReceiveAsync(sbm);
                r.IsSuccess.Should().BeTrue();
            }).AssertSuccess();

        // Assert - the reservation should now be confirmed.
        mr = await ms.GetAsync(referenceId);
        mr.Should().NotBeNull().And.HaveCount(3).And.AllSatisfy(m => m.StatusCode.Should().Be(MovementStatus.Confirmed));
    });
}