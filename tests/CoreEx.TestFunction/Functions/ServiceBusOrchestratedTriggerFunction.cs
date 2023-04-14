using Azure.Messaging.ServiceBus;
using CoreEx.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using System.Threading.Tasks;

namespace CoreEx.TestFunction.Functions
{
    public class ServiceBusOrchestratedTriggerFunction
    {
        private readonly ServiceBusOrchestratedSubscriber _subscriber;

        public ServiceBusOrchestratedTriggerFunction(ServiceBusOrchestratedSubscriber subscriber)
            => _subscriber = subscriber;

        [FunctionName("ServiceBusOrchestratedFunction")]
        [ExponentialBackoffRetry(3, "00:02:00", "00:30:00")]
        public Task RunAsync([ServiceBusTrigger("%" + nameof(TestSettings.OrchestratedQueueName) + "%", Connection = nameof(TestSettings.ServiceBusConnection))] ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions)
            => _subscriber.ReceiveAsync(message, messageActions);
    }
}