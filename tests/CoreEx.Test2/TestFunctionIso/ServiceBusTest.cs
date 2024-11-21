using CoreEx.Hosting.Work;
using CoreEx.TestFunctionIso;
using Microsoft.Extensions.DependencyInjection;
using UnitTestEx.Expectations;
using UnitTestEx;

namespace CoreEx.Test2.TestFunctionIso
{
    [TestFixture]
    public class ServiceBusTest
    {
        [Test]
        public async Task InvalidMessage()
        {
            using var test = FunctionTester.Create<Startup>();
            var actions = test.CreateWorkerServiceBusMessageActions();
            var message = test.CreateServiceBusMessageFromValue(new { Blah = true });

            var wo = test.Services.GetRequiredService<WorkStateOrchestrator>();
            await wo.CreateAsync(new WorkStateArgs("test") { Id = message.MessageId });

            test.ServiceBusTrigger<ServiceBusFunction>()
                .Run(f => f.Run(message, actions))
                .AssertSuccess();

            actions.AssertDeadLetter("EventDataDeserialization", "Invalid message; body was not provided, contained invalid JSON, or was incorrectly formatted: The JSON value could not be converted to System.String");

            var ws = await wo.GetAsync(message.MessageId);
            Assert.That(ws, Is.Not.Null);
            Assert.That(ws!.Status, Is.EqualTo(WorkStatus.Failed));
        }

        [Test]
        public async Task Complete_WorkStatus_Cancelled()
        {
            using var test = FunctionTester.Create<Startup>();
            var actions = test.CreateWorkerServiceBusMessageActions();
            var message = test.CreateServiceBusMessageFromValue("Bob");

            var wo = test.Services.GetRequiredService<WorkStateOrchestrator>();
            await wo.CreateAsync(new WorkStateArgs("test") { Id = message.MessageId });
            await wo.CancelAsync(message.MessageId, "No longer needed.");

            test.ServiceBusTrigger<ServiceBusFunction>()
                .ExpectLogContains("warn: Unable to process message as corresponding work state status is Cancelled: No longer needed.")
                .Run(f => f.Run(message, actions))
                .AssertSuccess();

            actions.AssertComplete();

            var ws = await wo.GetAsync(message.MessageId);
            Assert.That(ws, Is.Not.Null);
            Assert.That(ws!.Status, Is.EqualTo(WorkStatus.Cancelled));
        }

        [Test]
        public async Task Success()
        {
            using var test = FunctionTester.Create<Startup>();
            var actions = test.CreateWorkerServiceBusMessageActions();
            var message = test.CreateServiceBusMessageFromValue("Bob");

            var wo = test.Services.GetRequiredService<WorkStateOrchestrator>();
            await wo.CreateAsync(new WorkStateArgs("test") { Id = message.MessageId });

            test.ServiceBusTrigger<ServiceBusFunction>()
                .ExpectLogContains("Received message: Bob")
                .Run(f => f.Run(message, actions))
                .AssertSuccess();

            actions.AssertComplete();

            var ws = await wo.GetAsync(message.MessageId);
            Assert.That(ws, Is.Not.Null);
            Assert.That(ws!.Status, Is.EqualTo(WorkStatus.Completed));
        }
    }
}