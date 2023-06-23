﻿using Azure.Messaging.ServiceBus;
using CoreEx.FluentValidation;
using CoreEx.Azure.ServiceBus;
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
            => _subscriber.ReceiveAsync(message, messageActions, (ed, _) => _service.UpdateProductAsync(ed.Value!, ed.Value.Id!), validator: new ProductValidator().Wrap());
    }
}