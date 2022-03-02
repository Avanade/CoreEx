using Azure.Messaging.ServiceBus;
using CoreEx.Functions;
using CoreEx.Functions.FluentValidation;
using CoreEx.TestFunction.Models;
using CoreEx.TestFunction.Services;
using CoreEx.TestFunction.Validators;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using System.Threading.Tasks;

namespace CoreEx.TestFunction.Functions
{
    public class ServiceBusTriggerFunction
    {
        private readonly IServiceBusTriggerExecutor _executor;
        private readonly ProductService _service;

        public ServiceBusTriggerFunction(IServiceBusTriggerExecutor executor, ProductService service)
        {
            _executor = executor;
            _service = service;
        }

        [FunctionName("ServiceBusFunction")]
        [ExponentialBackoffRetry(3, "00:02:00", "00:30:00")]
        public async Task RunAsync([ServiceBusTrigger("%QueueName%", Connection = "ServiceBusConnection")] ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions)
            => await _executor.RunAsync<Product, ProductValidator>(message, messageActions, ed => _service.UpdateProductAsync(ed.Value)).ConfigureAwait(false);
    }
}