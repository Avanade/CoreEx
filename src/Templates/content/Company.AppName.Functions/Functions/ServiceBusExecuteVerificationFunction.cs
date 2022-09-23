using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using FluentValidation;
using CoreEx.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using Company.AppName.Business;
using Company.AppName.Business.Services;
using Company.AppName.Business.Validators;

namespace Company.AppName.Functions;

public class ServiceBusExecuteVerificationFunction
{
    private readonly ServiceBusSubscriber _subscriber;
    private readonly VerificationService _service;

    public ServiceBusExecuteVerificationFunction(ServiceBusSubscriber subscriber, VerificationService service)
    {
        _subscriber = subscriber;
        _service = service;
    }

    [FunctionName(nameof(ServiceBusExecuteVerificationFunction))]
    [ExponentialBackoffRetry(3, "00:02:00", "00:30:00")]
    public Task RunAsync([ServiceBusTrigger("%" + nameof(AppNameSettings.VerificationQueueName) + "%", Connection = nameof(AppNameSettings.ServiceBusConnection))] ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions)
        => _subscriber.ReceiveAsync(message, messageActions, ed => _service.VerifyAndPublish(ed.Value), validator: new EmployeeVerificationValidator().Wrap());
}