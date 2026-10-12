using CoreEx.Entities;
using CoreEx.Events;
using CoreEx.Events.Subscribing;
using CoreEx.Results;
using Microsoft.Extensions.Logging;

namespace CoreEx.Azure.Messaging.ServiceBus.Test.Unit.Subscribers;

[Subscribe("**.product.**")]
public class ProductSubscriber(ILogger<ProductSubscriber> logger) : SubscribedBase<Product>
{
    /// <summary>
    /// Gets or sets the signal completed once a message with an <c>Id</c> of 200 has started processing (allows tests to deterministically wait rather than delay).
    /// </summary>
    public static volatile TaskCompletionSource? Id200Processing;

    protected override async Task<Result> OnReceiveAsync(Product value, EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Received product with Id: {Id} and Sku: {Sku}.", value.Id, value.Sku);

        if (value.Id == 88)
            return Result.TransientError();
        else if (value.Id == 99)
            return Result.Fail(new InvalidOperationException("Oh no!"));
        else if (value.Id == 109)
            return Result.Fail(new DivideByZeroException("Might be poison?!"));
        else if (value.Id == 200)
        {
            Id200Processing?.TrySetResult();
            await Task.Delay(Timeout.Infinite, cancellationToken); // Simulates a long-running operation observing the receiver's own cancellation token (e.g. host/processor shutdown).
        }

        return Result.Success;
    }
}

public record class Product : IReadOnlyIdentifier<int>
{
    public int Id { get; init; }

    public required string Sku { get; init; }
}
