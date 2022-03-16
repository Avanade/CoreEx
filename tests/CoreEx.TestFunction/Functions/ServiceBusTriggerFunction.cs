using Azure.Messaging.ServiceBus;
using CoreEx.FluentValidation;
using CoreEx.Messaging.Azure.ServiceBus;
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
        private readonly ServiceBusSubscriber _subscriber;
        private readonly ProductService _service;

        public ServiceBusTriggerFunction(ServiceBusSubscriber subscriber, ProductService service)
        {
            _subscriber = subscriber;
            _service = service;
        }

        [FunctionName("ServiceBusFunction")]
        [ExponentialBackoffRetry(3, "00:02:00", "00:30:00")]
        public Task RunAsync([ServiceBusTrigger("%" + nameof(TestSettings.QueueName) + "%", Connection = nameof(TestSettings.ServiceBusConnection))] ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions)
            => _subscriber.ReceiveAsync<Product>(message, messageActions, ed => _service.UpdateProductAsync(ed.Validate<Product, ProductValidator>(), ed.Value.Id));
    }
}