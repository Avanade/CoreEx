using CoreEx.TestFunctionIso;
using UnitTestEx.NUnit;
using UnitTestEx.Expectations;

namespace CoreEx.Test2.TestFunctionIso
{
    [TestFixture]
    public class ServiceBusTest
    {
        [Test]
        public void InvalidMessage()
        {
            using var test = FunctionTester.Create<Startup>();
            var actions = test.CreateWorkerServiceBusMessageActions();
            var message = test.CreateServiceBusMessageFromValue(new { Blah = true });

            test.ServiceBusTrigger<ServiceBusFunction>()
                .Run(f => f.Run(message, actions))
                .AssertSuccess();

            actions.AssertDeadLetter("EventDataDeserialization", "Invalid message; body was not provided, contained invalid JSON, or was incorrectly formatted: The JSON value could not be converted to System.String");
        }

        [Test]
        public void Success()
        {
            using var test = FunctionTester.Create<Startup>();
            var actions = test.CreateWorkerServiceBusMessageActions();
            var message = test.CreateServiceBusMessageFromValue("Bob");

            test.ServiceBusTrigger<ServiceBusFunction>()
                .ExpectLogContains("Received message: Bob")
                .Run(f => f.Run(message, actions))
                .AssertSuccess();

            actions.AssertComplete();
        }
    }
}