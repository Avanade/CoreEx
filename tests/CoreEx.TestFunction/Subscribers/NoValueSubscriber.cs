using CoreEx.Events;
using CoreEx.Events.Subscribing;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.TestFunction.Subscribers
{
    [EventSubscriber("my.novalue")]
    public class NoValueSubscriber : SubscriberBase
    {
        private readonly ILogger _logger;

        public static HashSet<string> EventIds { get; } = new HashSet<string>();

        public NoValueSubscriber(ILogger<NoValueSubscriber> logger) => _logger = logger;

        public override Task ReceiveAsync(EventData @event, CancellationToken cancellationToken)
        {
            EventIds.Add(@event.Id!);
            _logger.LogInformation($"Message {@event.Id} was received.");
            return Task.CompletedTask;
        }
    }
}