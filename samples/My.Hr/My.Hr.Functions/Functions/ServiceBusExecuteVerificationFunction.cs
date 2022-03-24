using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using CoreEx.FluentValidation;
using CoreEx.Messaging.Azure.ServiceBus;
using CoreEx.WebApis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.OpenApi.Models;
using My.Hr.Business;
using My.Hr.Business.ServiceContracts;
using My.Hr.Business.Services;
using My.Hr.Business.Validators;

namespace My.Hr.Functions;

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
    public Task RunAsync([ServiceBusTrigger("%" + nameof(HrSettings.VerificationQueueName) + "%", Connection = nameof(HrSettings.ServiceBusConnection))] ServiceBusReceivedMessage message, ServiceBusMessageActions messageActions)
        => _subscriber.ReceiveAsync<EmployeeVerificationRequest>(message, messageActions, ed => _service.VerifyAndPublish(ed.Validate<EmployeeVerificationRequest, EmployeeVerificationValidator>()));
}