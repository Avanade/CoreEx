using CoreEx.Abstractions;
using CoreEx.Functions;
using CoreEx.TestFunction;
using CoreEx.TestFunction.Models;
using Microsoft.Azure.WebJobs.ServiceBus;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using UnitTestEx.NUnit;

namespace CoreEx.Test.Framework.Functions
{
    [TestFixture]
    public class ServiceBusTriggerExecutorTest
    {
        [Test]
        public void RunAsync_NullMessage_Error()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<ServiceBusTriggerExecutor>()
                .Run(f => f.RunAsync<Product>(null, null, null))
                .AssertException<ArgumentNullException>("Value cannot be null. (Parameter 'message')");
        }

        [Test]
        public void RunAsync_NullMessageAction_Error()
        {
            using var test = FunctionTester.Create<Startup>();
            test.Type<ServiceBusTriggerExecutor>()
                .Run(f => f.RunAsync<Product>(test.CreateServiceBusMessage(new Product()), null, null))
                .AssertException<ArgumentNullException>("Value cannot be null. (Parameter 'messageActions')");
        }

        [Test]
        public void RunAsync_NullFunction_Error()
        {
            var actionsMock = new Mock<ServiceBusMessageActions>();
            using var test = FunctionTester.Create<Startup>();
            test.Type<ServiceBusTriggerExecutor>()
                .Run(f => f.RunAsync<Product>(test.CreateServiceBusMessage(new Product()), actionsMock.Object, null))
                .AssertException<ArgumentNullException>("Value cannot be null. (Parameter 'function')");
        }

        [Test]
        public void RunAsync_Message_Null()
        {
            using var test = FunctionTester.Create<Startup>();
            var actionsMock = new Mock<ServiceBusMessageActions>();
            var message = test.CreateServiceBusMessage<Product>(null);

            test.Type<ServiceBusTriggerExecutor>()
                .Run(f => f.RunAsync<Product>(message, actionsMock.Object, ed => throw new InvalidOperationException("Should not get here.")))
                .AssertSuccess();

            actionsMock.Verify(m => m.DeadLetterMessageAsync(message, ErrorType.ValidationError.ToString(), "CoreEx.ValidationException: Invalid message: body was not provided, contained invalid JSON, or was incorrectly formatted: Value is mandatory.", default), Times.Once);
            actionsMock.VerifyNoOtherCalls();
        }

        [Test]
        public void RunAsync_Message_ValidationException()
        {
            using var test = FunctionTester.Create<Startup>();
            var actionsMock = new Mock<ServiceBusMessageActions>();
            var message = test.CreateServiceBusMessage<Product>(new Product { Id = "A", Name = "B", Price = 1.99m });

            test.Type<ServiceBusTriggerExecutor>()
                .Run(f => f.RunAsync<Product>(message, actionsMock.Object, ed => throw new ValidationException()))
                .AssertSuccess();

            actionsMock.Verify(m => m.DeadLetterMessageAsync(message, ErrorType.ValidationError.ToString(), It.IsAny<string>(), default), Times.Once);
            actionsMock.VerifyNoOtherCalls();
        }

        [Test]
        public void RunAsync_Message_NotFoundException()
        {
            using var test = FunctionTester.Create<Startup>();
            var actionsMock = new Mock<ServiceBusMessageActions>();
            var message = test.CreateServiceBusMessage<Product>(new Product { Id = "A", Name = "B", Price = 1.99m });

            test.Type<ServiceBusTriggerExecutor>()
                .Run(f => f.RunAsync<Product>(message, actionsMock.Object, ed => throw new NotFoundException()))
                .AssertSuccess();

            actionsMock.Verify(m => m.DeadLetterMessageAsync(message, ErrorType.NotFoundError.ToString(), It.IsAny<string>(), default), Times.Once);
            actionsMock.VerifyNoOtherCalls();
        }

        [Test]
        public void RunAsync_Message_TransientException()
        {
            using var test = FunctionTester.Create<Startup>();
            var actionsMock = new Mock<ServiceBusMessageActions>();
            var message = test.CreateServiceBusMessage<Product>(new Product { Id = "A", Name = "B", Price = 1.99m });

            test.Type<ServiceBusTriggerExecutor>()
                .Run(f => f.RunAsync<Product>(message, actionsMock.Object, ed => throw new TransientException()))
                .AssertException<TransientException>();

            actionsMock.VerifyNoOtherCalls();
        }

        [Test]
        public void RunAsync_Message_UnhandledException()
        {
            using var test = FunctionTester.Create<Startup>();
            var actionsMock = new Mock<ServiceBusMessageActions>();
            var message = test.CreateServiceBusMessage<Product>(new Product { Id = "A", Name = "B", Price = 1.99m });

            test.Type<ServiceBusTriggerExecutor>()
                .Run(f => f.RunAsync<Product>(message, actionsMock.Object, ed => throw new DivideByZeroException()))
                .AssertSuccess();

            actionsMock.Verify(m => m.DeadLetterMessageAsync(message, ServiceBusTriggerExecutor.DeadLetterUnhandledReason, It.IsAny<string>(), default), Times.Once);
            actionsMock.VerifyNoOtherCalls();
        }

        [Test]
        public void RunAsync_Success()
        {
            var p = new Product { Id = "A", Name = "B", Price = 1.99m };
            var actionsMock = new Mock<ServiceBusMessageActions>();

            using var test = FunctionTester.Create<Startup>();
            test.Type<ServiceBusTriggerExecutor>()
                .Run(f => f.RunAsync<Product>(test.CreateServiceBusMessage(p), actionsMock.Object, ed => { ObjectComparer.Assert(ed.Value, new Product { Id = "A", Name = "B", Price = 1.99m }); return Task.CompletedTask; }))
                .AssertSuccess();
        }

        [Test]
        public void RunAsync_Success_CorrelationId()
        {
            var p = new Product { Id = "A", Name = "B", Price = 1.99m };
            var actionsMock = new Mock<ServiceBusMessageActions>();

            using var test = FunctionTester.Create<Startup>();
            var sbm = test.CreateServiceBusMessage(p, m => m.Properties.CorrelationId = new Azure.Core.Amqp.AmqpMessageId("corr-id"));

            test.Type<ServiceBusTriggerExecutor>()
                .Run(f => f.RunAsync<Product>(sbm, actionsMock.Object, ed => { ObjectComparer.Assert("corr-id", ExecutionContext.Current.CorrelationId); return Task.CompletedTask; }))
                .AssertSuccess();
        }
    }
}