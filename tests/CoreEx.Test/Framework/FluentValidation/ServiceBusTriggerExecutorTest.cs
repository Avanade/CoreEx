using CoreEx.Abstractions;
using CoreEx.TestFunction;
using CoreEx.TestFunction.Functions;
using CoreEx.TestFunction.Models;
using Microsoft.Azure.WebJobs.ServiceBus;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using UnitTestEx.NUnit;

namespace CoreEx.Test.Framework.FluentValidation
{
    [TestFixture]
    public class ServiceBusTriggerExecutorTest
    {
        [Test]
        public void ReceiveAsync_ValidationException_Validation()
        {
            using var test = FunctionTester.Create<Startup>();
            var actionsMock = new Mock<ServiceBusMessageActions>();
            var message = test.CreateServiceBusMessage(new Product { Id = "Zed", Name = "is dead", Price = 1.99m });

            test.ServiceBusTrigger<ServiceBusTriggerFunction>()
                .Run(s => s.RunAsync(message, actionsMock.Object))
                .AssertSuccess();

            actionsMock.Verify(m => m.DeadLetterMessageAsync(message, It.IsAny<Dictionary<string, object?>>(), ErrorType.ValidationError.ToString(), It.IsAny<string>(), default), Times.Once);
            actionsMock.VerifyNoOtherCalls();
        }
    }
}