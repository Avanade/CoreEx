using Azure.Messaging.ServiceBus;
using CoreEx.Azure.ServiceBus;
using CoreEx.Results;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace CoreEx.TestFunctionIso
{
    public class ServiceBusFunction(ServiceBusSubscriber subscriber, ILogger<ServiceBusFunction> logger)
    {
        private readonly ServiceBusSubscriber _subscriber = subscriber;
        private readonly ILogger _logger = logger;

        [Function(nameof(ServiceBusFunction))]
        public Task Run([ServiceBusTrigger("test-queue", Connection = "ServiceBusConnectionString")] ServiceBusReceivedMessage message, ServiceBusMessageActions sbma)
            => _subscriber.ReceiveAsync<string>(message, sbma, (@event, args) =>
            {
                _logger.LogInformation($"Received message: {@event.Value}");
                return Result.SuccessTask;
            });
    }
}