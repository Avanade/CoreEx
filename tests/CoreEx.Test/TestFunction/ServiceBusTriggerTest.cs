using CoreEx.Abstractions;
using CoreEx.Events;
using CoreEx.Events.Subscribing;
using CoreEx.Json;
using CoreEx.TestFunction;
using CoreEx.TestFunction.Functions;
using CoreEx.TestFunction.Models;
using Microsoft.Azure.WebJobs.ServiceBus;
using Moq;
using NUnit.Framework;
using System.Net;
using System.Net.Http;
using UnitTestEx.NUnit;

namespace CoreEx.Test.TestFunction
{
    [TestFixture]
    public class ServiceBusTriggerTest
    {
        [Test]
        public void NullMessage()
        {
            using var test = FunctionTester.Create<Startup>();
            var actionsMock = new Mock<ServiceBusMessageActions>();
            var message = test.CreateServiceBusMessage<Product>(null!);

            test.ServiceBusTrigger<ServiceBusTriggerFunction>()
                .Run(f => f.RunAsync(message, actionsMock.Object))
                .AssertSuccess();

            actionsMock.Verify(m => m.DeadLetterMessageAsync(message, ErrorType.ValidationError.ToString(), It.IsNotNull<string>(), default), Times.Once);
            actionsMock.VerifyNoOtherCalls();
        }

        [Test]
        public void InvalidMessage()
        {
            using var test = FunctionTester.Create<Startup>();
            var actionsMock = new Mock<ServiceBusMessageActions>();
            var message = test.CreateServiceBusMessage("<xml/>");

            test.ServiceBusTrigger<ServiceBusTriggerFunction>()
                .Run(f => f.RunAsync(message, actionsMock.Object))
                .AssertSuccess();

            actionsMock.Verify(m => m.DeadLetterMessageAsync(message, EventSubscriberExceptionSource.EventDataDeserialization.ToString(), It.IsNotNull<string>(), default), Times.Once);
            actionsMock.VerifyNoOtherCalls();
        }

        [Test]
        public void InvalidMessage_Newtonsoft()
        {
            using var test = FunctionTester.Create<Startup>()
                .ReplaceScoped<IJsonSerializer, CoreEx.Newtonsoft.Json.JsonSerializer>()
                .ReplaceScoped<IEventSerializer, CoreEx.Newtonsoft.Json.EventDataSerializer>();

            var actionsMock = new Mock<ServiceBusMessageActions>();
            var message = test.CreateServiceBusMessage("<xml/>");

            test.ServiceBusTrigger<ServiceBusTriggerFunction>()
                .Run(f => f.RunAsync(message, actionsMock.Object))
                .AssertSuccess();

            actionsMock.Verify(m => m.DeadLetterMessageAsync(message, EventSubscriberExceptionSource.EventDataDeserialization.ToString(), It.IsNotNull<string>(), default), Times.Once);
            actionsMock.VerifyNoOtherCalls();
        }

        [Test]
        public void InvalidValue()
        {
            using var test = FunctionTester.Create<Startup>();
            var actionsMock = new Mock<ServiceBusMessageActions>();
            var message = test.CreateServiceBusMessage(new { id = "A", price = 1.99m });

            test.ServiceBusTrigger<ServiceBusTriggerFunction>()
                .Run(f => f.RunAsync(message, actionsMock.Object))
                .AssertSuccess();

            actionsMock.Verify(m => m.DeadLetterMessageAsync(message, ErrorType.ValidationError.ToString(), It.IsNotNull<string>(), default), Times.Once);
            actionsMock.VerifyNoOtherCalls();
        }

        [Test]
        public void InvalidValue_Newtonsoft()
        {
            using var test = FunctionTester.Create<Startup>()
                .ReplaceScoped<IJsonSerializer, CoreEx.Newtonsoft.Json.JsonSerializer>()
                .ReplaceScoped<IEventSerializer, CoreEx.Newtonsoft.Json.EventDataSerializer>();

            var actionsMock = new Mock<ServiceBusMessageActions>();
            var message = test.CreateServiceBusMessage(new { id = "A", price = 1.99m });

            test.ServiceBusTrigger<ServiceBusTriggerFunction>()
                .Run(f => f.RunAsync(message, actionsMock.Object))
                .AssertSuccess();

            actionsMock.Verify(m => m.DeadLetterMessageAsync(message, ErrorType.ValidationError.ToString(), It.IsNotNull<string>(), default), Times.Once);
            actionsMock.VerifyNoOtherCalls();
        }

        [Test]
        public void TransientError()
        {
            var mcf = MockHttpClientFactory.Create();
            var mc = mcf.CreateClient("Backend", "https://backend/");
            mc.Request(HttpMethod.Post, "products").WithJsonBody(new BackendProduct { Code = "A", Description = "B", RetailPrice = 1.99m }).Respond.With(HttpStatusCode.InternalServerError);

            using var test = FunctionTester.Create<Startup>();
            var actionsMock = new Mock<ServiceBusMessageActions>();
            var message = test.CreateServiceBusMessage(new { id = "A", name = "B", price = 1.99m });

            test.ReplaceHttpClientFactory(mcf)
                .ServiceBusTrigger<ServiceBusTriggerFunction>()
                .Run(f => f.RunAsync(message, actionsMock.Object))
                .AssertException<EventSubscriberException>("Response status code was InternalServerError >= 500.");

            mc.Verify();
            actionsMock.VerifyNoOtherCalls();
        }

        [Test]
        public void UnhandledError()
        {
            var mcf = MockHttpClientFactory.Create();
            var mc = mcf.CreateClient("Backend", "https://backend/");
            mc.Request(HttpMethod.Post, "products").WithJsonBody(new BackendProduct { Code = "A", Description = "B", RetailPrice = 1.99m }).Respond.With(HttpStatusCode.Ambiguous);

            using var test = FunctionTester.Create<Startup>();
            var actionsMock = new Mock<ServiceBusMessageActions>();
            var message = test.CreateServiceBusMessage(new { id = "A", name = "B", price = 1.99m });

            test.ReplaceHttpClientFactory(mcf)
                .ServiceBusTrigger<ServiceBusTriggerFunction>()
                .Run(f => f.RunAsync(message, actionsMock.Object))
                .AssertSuccess();

            mc.Verify();
            actionsMock.Verify(m => m.DeadLetterMessageAsync(message, ErrorType.UnhandledError.ToString(), It.IsNotNull<string>(), default), Times.Once);
            actionsMock.VerifyNoOtherCalls();
        }

        [Test]
        public void Success()
        {
            var mcf = MockHttpClientFactory.Create();
            var mc = mcf.CreateClient("Backend", "https://backend/");
            mc.Request(HttpMethod.Post, "products").WithJsonBody(new BackendProduct { Code = "A", Description = "B", RetailPrice = 1.99m }).Respond.WithJson(new BackendProduct { Code = "AX", Description = "BX", RetailPrice = 10.99m });

            using var test = FunctionTester.Create<Startup>();
            var actionsMock = new Mock<ServiceBusMessageActions>();
            var message = test.CreateServiceBusMessage(new { id = "A", name = "B", price = 1.99m });

            test.ReplaceHttpClientFactory(mcf)
                .ServiceBusTrigger<ServiceBusTriggerFunction>()
                .Run(f => f.RunAsync(message, actionsMock.Object))
                .AssertSuccess();

            mc.Verify();
            actionsMock.Verify(m => m.CompleteMessageAsync(message, default), Times.Once);
            actionsMock.VerifyNoOtherCalls();
        }

        [Test]
        public void Success_Newtonsoft()
        {
            var mcf = MockHttpClientFactory.Create();
            var mc = mcf.CreateClient("Backend", "https://backend/");
            mc.Request(HttpMethod.Post, "products").WithJsonBody(new BackendProduct { Code = "A", Description = "B", RetailPrice = 1.99m }).Respond.WithJson(new BackendProduct { Code = "AX", Description = "BX", RetailPrice = 10.99m });

            using var test = FunctionTester.Create<Startup>()
                .ReplaceScoped<IJsonSerializer, CoreEx.Newtonsoft.Json.JsonSerializer>()
                .ReplaceScoped<IEventSerializer, CoreEx.Newtonsoft.Json.EventDataSerializer>();

            var actionsMock = new Mock<ServiceBusMessageActions>();
            var message = test.CreateServiceBusMessage(new { id = "A", name = "B", price = 1.99m });

            test.ReplaceHttpClientFactory(mcf)
                .ServiceBusTrigger<ServiceBusTriggerFunction>()
                .Run(f => f.RunAsync(message, actionsMock.Object))
                .AssertSuccess();

            mc.Verify();
            actionsMock.Verify(m => m.CompleteMessageAsync(message, default), Times.Once);
            actionsMock.VerifyNoOtherCalls();
        }
    }
}