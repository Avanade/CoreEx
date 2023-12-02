using CoreEx.Abstractions;
using CoreEx.Azure.ServiceBus;
using CoreEx.Events;
using CoreEx.TestFunction;
using CoreEx.TestFunction.Models;
using Microsoft.Azure.WebJobs.ServiceBus;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnitTestEx.NUnit;

namespace CoreEx.Test.Framework.Messaging.Azure.ServiceBus
{
    [TestFixture]
    public class ServiceBusSubscriberTest
    {
        [Test]
        public void ReceiveAsync_ValidationException()
        {
            using var test = FunctionTester.Create<Startup>();
            var actionsMock = new Mock<ServiceBusMessageActions>();
            var message = test.CreateServiceBusMessageFromValue<Product?>(null);

            test.Type<ServiceBusSubscriber>()
                .Run(s => s.ReceiveAsync<Product>(message, actionsMock.Object, (ed, _) => throw new InvalidOperationException("Should not get here.")))
                .AssertSuccess();

            actionsMock.Verify(m => m.DeadLetterMessageAsync(message, It.IsAny<Dictionary<string, object?>>(), ErrorType.ValidationError.ToString(), It.IsAny<string>(), default), Times.Once);
            actionsMock.VerifyNoOtherCalls();
        }

        [Test]
        public void ReceiveAsync_TransientException()
        {
            using var test = FunctionTester.Create<Startup>();
            var actionsMock = new Mock<ServiceBusMessageActions>();
            var message = test.CreateServiceBusMessageFromValue(new Product { Id = "A", Price = 1.99m });

            test.Type<ServiceBusSubscriber>()
                .Run(s => s.ReceiveAsync<Product>(message, actionsMock.Object, (ed, _) => throw new TransientException()))
                .AssertException<EventSubscriberException>("A transient error has occurred; please try again.");

            actionsMock.VerifyNoOtherCalls();
        }

        [Test]
        public void ReceiveAsync_UnhandledException()
        {
            using var test = FunctionTester.Create<Startup>();
            var actionsMock = new Mock<ServiceBusMessageActions>();
            var message = test.CreateServiceBusMessageFromValue(new Product { Id = "A", Price = 1.99m });

            test.Type<ServiceBusSubscriber>()
                .Run(s => s.ReceiveAsync<Product>(message, actionsMock.Object, (ed, _) => throw new DivideByZeroException()))
                .AssertSuccess();

            actionsMock.Verify(m => m.DeadLetterMessageAsync(message, It.IsAny<Dictionary<string, object?>>(), ErrorType.UnhandledError.ToString(), It.IsAny<string>(), default), Times.Once);
            actionsMock.VerifyNoOtherCalls();
        }

        [Test]
        public void ReceiveAsync_Success()
        {
            using var test = FunctionTester.Create<Startup>();
            var actionsMock = new Mock<ServiceBusMessageActions>();
            var message = test.CreateServiceBusMessageFromValue(new Product { Id = "A", Price = 1.99m });

            test.Type<ServiceBusSubscriber>()
                .Run(s => s.ReceiveAsync<Product>(message, actionsMock.Object, (ed, _) => Task.CompletedTask))
                .AssertSuccess();

            actionsMock.Verify(m => m.CompleteMessageAsync(message, default), Times.Once);
            actionsMock.VerifyNoOtherCalls();
        }
    }
}