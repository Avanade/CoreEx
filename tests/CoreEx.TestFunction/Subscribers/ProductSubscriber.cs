using CoreEx.Events;
using CoreEx.Events.Subscribing;
using CoreEx.FluentValidation;
using CoreEx.Results;
using CoreEx.TestFunction.Models;
using CoreEx.TestFunction.Validators;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.TestFunction.Subscribers
{
    [EventSubscriber("my.product")]
    public class ProductSubscriber : SubscriberBase<Product>
    {
        private readonly ILogger _logger;

        public static HashSet<string> EventIds { get; } = new HashSet<string>();

        public ProductSubscriber(ILogger<NoValueSubscriber> logger)
        {
            _logger = logger;
            ValueValidator = new ProductValidator().Wrap();
        }

        public async override Task<Result> ReceiveAsync(EventData<Product> @event, EventSubscriberArgs args, CancellationToken cancellationToken)
        {
            await Task.CompletedTask.ConfigureAwait(false);
            EventIds.Add(@event.Id!);
            _logger.LogInformation($"Message {@event.Id} for Product {@event.Value.Id} was received.");

            if (@event.Value.Id == "PS5")
                return Result.TransientError($"{@event.Value.Name} is currently not permissable; please try again later.");

            return Result.Success;
        }
    }
}