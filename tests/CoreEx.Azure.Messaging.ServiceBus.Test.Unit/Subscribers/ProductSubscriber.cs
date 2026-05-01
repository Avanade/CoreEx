using CoreEx.Entities;
using CoreEx.Events;
using CoreEx.Events.Subscribing;
using CoreEx.Results;
using Microsoft.Extensions.Logging;

namespace CoreEx.Azure.Messaging.ServiceBus.Test.Unit.Subscribers;

[Subscribe("**.product.**")]
public class ProductSubscriber(ILogger<ProductSubscriber> logger) : SubscribedBase<Product>
{
    protected override Task<Result> OnReceiveAsync(Product value, EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Received product with Id: {Id} and Sku: {Sku}.", value.Id, value.Sku);

        if (value.Id == 88)
            return Result.TransientError().AsTask();
        else if (value.Id == 99)
            return Result.Fail(new InvalidOperationException("Oh no!")).AsTask();
        else if (value.Id == 109)
            return Result.Fail(new DivideByZeroException("Might be poison?!")).AsTask();

        return Result.SuccessTask;
    }
}

public record class Product : IReadOnlyIdentifier<int>
{
    public int Id { get; init; }

    public required string Sku { get; init; }
}