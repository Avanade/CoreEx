using CoreEx.Abstractions;
using CoreEx.Functions;
using CoreEx.Functions.FluentValidation;
using CoreEx.TestFunction;
using CoreEx.TestFunction.Models;
using CoreEx.TestFunction.Validators;
using Microsoft.Azure.WebJobs.ServiceBus;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using UnitTestEx.NUnit;

namespace CoreEx.Test.Framework.Functions.FluentValidation
{
    [TestFixture]
    public class ServiceBusTriggerExecutorTest
    {
        [Test]
        public void RunAsync_Message_ValidationException()
        {
            using var test = FunctionTester.Create<Startup>();
            var actionsMock = new Mock<ServiceBusMessageActions>();
            var message = test.CreateServiceBusMessage<Product>(new Product { Id = "A", Price = 1.99m });

            test.Type<ServiceBusTriggerExecutor>()
                .Run(f => f.RunAsync<Product, ProductValidator>(message, actionsMock.Object, ed => throw new InvalidOperationException("Should not get here.")))
                .AssertSuccess();

            actionsMock.Verify(m => m.DeadLetterMessageAsync(message, ErrorType.ValidationError.ToString(), It.IsAny<string>(), default), Times.Once);
            actionsMock.VerifyNoOtherCalls();
        }
    }
}