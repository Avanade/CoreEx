using Azure.Core.Amqp;
using CoreEx.Abstractions;
using CoreEx.Azure.ServiceBus;
using CoreEx.Events;
using CoreEx.Events.Subscribing;
using CoreEx.TestFunction;
using CoreEx.TestFunction.Functions;
using CoreEx.TestFunction.Models;
using CoreEx.TestFunction.Subscribers;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using System;
using UnitTestEx.Abstractions;
using UnitTestEx.NUnit;
using Az = Azure.Messaging.ServiceBus;

namespace CoreEx.Test.TestFunction
{
    [TestFixture]
    public class ServiceBusOrchestratedTriggerFunctionTest
    {
        [Test]
        public void NotSubscribed()
        {
            using var test = FunctionTester.Create<Startup>();
            test.ReplaceScoped<ServiceBusSender>();
            var actionsMock = new Mock<ServiceBusMessageActions>();
            var message = test.CreateServiceBusMessageFromEvent(new EventData { Id = 1.ToGuid().ToString(), Subject = "my.unknown" });

            test.ServiceBusTrigger<ServiceBusOrchestratedTriggerFunction>()
                .Run(f => f.RunAsync(message, actionsMock.Object))
                .AssertSuccess();

            actionsMock.Verify(m => m.DeadLetterMessageAsync(message, EventSubscriberExceptionSource.OrchestratorNotSubscribed.ToString(), It.IsNotNull<string>(), default), Times.Once);
            actionsMock.VerifyNoOtherCalls();
        }

        [Test]
        public void NoValue_Success()
        {
            using var test = FunctionTester.Create<Startup>();
            test.ReplaceScoped<ServiceBusSender>();
            var actionsMock = new Mock<ServiceBusMessageActions>();
            var message = test.CreateServiceBusMessageFromEvent(new EventData { Id = 101.ToGuid().ToString(), Subject = "my.novalue" });

            test.ServiceBusTrigger<ServiceBusOrchestratedTriggerFunction>()
                .Run(f => f.RunAsync(message, actionsMock.Object))
                .AssertSuccess();

            actionsMock.Verify(m => m.CompleteMessageAsync(message, default), Times.Once);
            Assert.IsTrue(NoValueSubscriber.EventIds.Contains(101.ToGuid().ToString()));
        }

        [Test]
        public void Product_ValueIsRequired()
        {
            using var test = FunctionTester.Create<Startup>();
            var actionsMock = new Mock<ServiceBusMessageActions>();
            var message = test.CreateServiceBusMessageFromEvent(new EventData<Product> { Id = 201.ToGuid().ToString(), Subject = "my.Product", Value = null! });

            test.ServiceBusTrigger<ServiceBusOrchestratedTriggerFunction>()
                .Run(f => f.RunAsync(message, actionsMock.Object))
                .AssertSuccess();

            actionsMock.Verify(m => m.DeadLetterMessageAsync(message, ErrorType.ValidationError.ToString(), It.IsNotNull<string>(), default), Times.Once);
            actionsMock.VerifyNoOtherCalls();
        }

        [Test]
        public void Product_ValueIsInvalid()
        {
            using var test = FunctionTester.Create<Startup>();
            var actionsMock = new Mock<ServiceBusMessageActions>();
            var message = test.CreateServiceBusMessageFromEvent(new EventData<Product> { Id = 201.ToGuid().ToString(), Subject = "my.Product", Value = new Product() });

            test.ServiceBusTrigger<ServiceBusOrchestratedTriggerFunction>()
                .Run(f => f.RunAsync(message, actionsMock.Object))
                .AssertSuccess();

            actionsMock.Verify(m => m.DeadLetterMessageAsync(message, ErrorType.ValidationError.ToString(), It.IsNotNull<string>(), default), Times.Once);
            actionsMock.VerifyNoOtherCalls();
        }

        [Test]
        public void Product_Success()
        {
            using var test = FunctionTester.Create<Startup>();
            var actionsMock = new Mock<ServiceBusMessageActions>();
            var message = test.CreateServiceBusMessageFromEvent(new EventData<Product> { Id = 201.ToGuid().ToString(), Subject = "my.Product", Value = new Product { Id = "XBX", Name = "XBox Series X", Price = 999.99m } });

            test.ServiceBusTrigger<ServiceBusOrchestratedTriggerFunction>()
                .Run(f => f.RunAsync(message, actionsMock.Object))
                .AssertSuccess();

            actionsMock.Verify(m => m.CompleteMessageAsync(message, default), Times.Once);
            Assert.IsTrue(ProductSubscriber.EventIds.Contains(201.ToGuid().ToString()));
        }

        [Test]
        public void Product_Transient()
        {
            using var test = FunctionTester.Create<Startup>();
            var actionsMock = new Mock<ServiceBusMessageActions>();
            var message = test.CreateServiceBusMessageFromEvent(new EventData<Product> { Id = 202.ToGuid().ToString(), Subject = "my.Product", Value = new Product { Id = "PS5", Name = "Sony Playstation 5", Price = 1099.99m } });

            test.ServiceBusTrigger<ServiceBusOrchestratedTriggerFunction>()
                .Run(f => f.RunAsync(message, actionsMock.Object))
                .AssertException<EventSubscriberException>("Sony Playstation 5 is currently not permissable; please try again later.");

            actionsMock.VerifyNoOtherCalls();
            Assert.IsTrue(ProductSubscriber.EventIds.Contains(202.ToGuid().ToString()));
        }
    }

    public static class EM
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Needed for now!")]
        public static Az.ServiceBusReceivedMessage CreateServiceBusMessageFromAmqp(this TesterBase tester, AmqpAnnotatedMessage message)
        {
            if (message == null) throw new ArgumentNullException("message");

            message.Header.DeliveryCount = 1;
            message.Header.Durable = true;
            message.Header.Priority = 1;
            message.Header.TimeToLive = TimeSpan.FromSeconds(60);

            var t = typeof(Az.ServiceBusReceivedMessage);
            var c = t.GetConstructor(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, new Type[] { typeof(AmqpAnnotatedMessage) }, null);
            return c == null
                ? throw new InvalidOperationException($"'{typeof(Az.ServiceBusReceivedMessage).Name}' constructor that accepts Type '{typeof(AmqpAnnotatedMessage).Name}' parameter was not found.")
                : (Az.ServiceBusReceivedMessage)c.Invoke(new object?[] { message });
        }

        public static Az.ServiceBusReceivedMessage CreateServiceBusMessageFromEvent(this TesterBase tester, EventData @event)
        {
            if (@event == null) throw new ArgumentNullException("@event");
            var message = (tester.Services.GetService<EventDataToServiceBusConverter>() ?? new EventDataToServiceBusConverter()).Convert(@event);
            return CreateServiceBusMessageFromAmqp(tester, message.GetRawAmqpMessage());
        }
    }
}