using CoreEx.Azure.ServiceBus;
using CoreEx.Events;
using CoreEx.TestFunction;
using CoreEx.TestFunction.Functions;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnitTestEx.NUnit;

namespace CoreEx.Test.TestFunction
{
    [TestFixture]
    public class ServiceBusSubsciberTest
    {
        [Test]
        public void NoAbandonOnTransient()
        {
            using var test = FunctionTester.Create<Startup>();
            var actions = test.CreateServiceBusMessageActions();
            var message = test.CreateServiceBusMessage(new { id = "A", name = "B", price = 1.99m });

            var sbs = test.Services.GetRequiredService<ServiceBusSubscriber>();
            sbs.AbandonOnTransient = false;

            Assert.ThrowsAsync<EventSubscriberException>(() => sbs.ReceiveAsync(message, actions, (_, _) => throw new TransientException("Retry again please.")));

            actions.AssertRenew(0);
            actions.AssertNone();
        }

        [Test]
        public async Task AbandonOnTransient()
        {
            using var test = FunctionTester.Create<Startup>();
            var actions = test.CreateServiceBusMessageActions();
            var message = test.CreateServiceBusMessage(new { id = "A", name = "B", price = 1.99m });

            var sbs = test.Services.GetRequiredService<ServiceBusSubscriber>();
            sbs.AbandonOnTransient = true;

            await sbs.ReceiveAsync(message, actions, (_, _) => throw new TransientException("Retry again please."));

            actions.AssertRenew(0);
            actions.AssertAbandon();

            Assert.IsNotNull(actions.PropertiesModified);
            Assert.AreEqual(actions.PropertiesModified!["SubscriberAbandonReason"], "Retry again please.");
        }

        [Test]
        public async Task RetryDelay_DeliveryCount1()
        {
            using var test = FunctionTester.Create<Startup>();
            var actions = test.CreateServiceBusMessageActions();
            var message = test.CreateServiceBusMessage(new { id = "A", name = "B", price = 1.99m });

            var sbs = test.Services.GetRequiredService<ServiceBusSubscriber>();
            sbs.AbandonOnTransient = true;
            sbs.RetryDelay = TimeSpan.FromSeconds(1);

            var sw = Stopwatch.StartNew();
            await sbs.ReceiveAsync(message, actions, (_, _) => throw new TransientException("Retry again please."));
            sw.Stop();

            Assert.GreaterOrEqual(sw.ElapsedMilliseconds, 950);

            actions.AssertRenew(1);
            actions.AssertAbandon();

            Assert.IsNotNull(actions.PropertiesModified);
            Assert.AreEqual(actions.PropertiesModified!["SubscriberAbandonReason"], "Retry again please.");
        }

        [Test]
        public async Task RetryDelay_DeliveryCount2()
        {
            using var test = FunctionTester.Create<Startup>();
            var actions = test.CreateServiceBusMessageActions();
            var message = test.CreateServiceBusMessage(new { id = "A", name = "B", price = 1.99m }, m => m.Header.DeliveryCount = 2);

            var sbs = test.Services.GetRequiredService<ServiceBusSubscriber>();
            sbs.AbandonOnTransient = true;
            sbs.RetryDelay = TimeSpan.FromSeconds(1);

            var sw = Stopwatch.StartNew();
            await sbs.ReceiveAsync(message, actions, (_, _) => throw new TransientException("Retry again please."));
            sw.Stop();

            Assert.GreaterOrEqual(sw.ElapsedMilliseconds, 1950);

            actions.AssertRenew(1);
            actions.AssertAbandon();

            Assert.IsNotNull(actions.PropertiesModified);
            Assert.AreEqual(actions.PropertiesModified!["SubscriberAbandonReason"], "Retry again please.");
        }

        [Test]
        public async Task RetryDelay_DeliveryCount2_WithMax()
        {
            using var test = FunctionTester.Create<Startup>();
            var actions = test.CreateServiceBusMessageActions();
            var message = test.CreateServiceBusMessage(new { id = "A", name = "B", price = 1.99m }, m => m.Header.DeliveryCount = 2);

            var sbs = test.Services.GetRequiredService<ServiceBusSubscriber>();
            sbs.AbandonOnTransient = true;
            sbs.RetryDelay = TimeSpan.FromSeconds(1);
            sbs.MaxRetryDelay = TimeSpan.FromMilliseconds(1100);

            var sw = Stopwatch.StartNew();
            await sbs.ReceiveAsync(message, actions, (_, _) => throw new TransientException("Retry again please."));
            sw.Stop();

            Assert.GreaterOrEqual(sw.ElapsedMilliseconds, 1050);

            actions.AssertRenew(1);
            actions.AssertAbandon();

            Assert.IsNotNull(actions.PropertiesModified);
            Assert.AreEqual(actions.PropertiesModified!["SubscriberAbandonReason"], "Retry again please.");
        }

        [Test]
        public async Task RetryDelay_DeliveryCount2_WithMaxOnly()
        {
            using var test = FunctionTester.Create<Startup>();
            var actions = test.CreateServiceBusMessageActions();
            var message = test.CreateServiceBusMessage(new { id = "A", name = "B", price = 1.99m }, m => m.Header.DeliveryCount = 2);

            var sbs = test.Services.GetRequiredService<ServiceBusSubscriber>();
            sbs.AbandonOnTransient = true;
            sbs.MaxRetryDelay = TimeSpan.FromMilliseconds(600);

            var sw = Stopwatch.StartNew();
            await sbs.ReceiveAsync(message, actions, (_, _) => throw new TransientException("Retry again please."));
            sw.Stop();

            Assert.GreaterOrEqual(sw.ElapsedMilliseconds, 550);

            actions.AssertRenew(1);
            actions.AssertAbandon();

            Assert.IsNotNull(actions.PropertiesModified);
            Assert.AreEqual(actions.PropertiesModified!["SubscriberAbandonReason"], "Retry again please.");
        }

        [Test]
        public async Task MaxDeliveryCount_LessThan()
        {
            using var test = FunctionTester.Create<Startup>();
            var actions = test.CreateServiceBusMessageActions();
            var message = test.CreateServiceBusMessage(new { id = "A", name = "B", price = 1.99m }, m => m.Header.DeliveryCount = 2);

            var sbs = test.Services.GetRequiredService<ServiceBusSubscriber>();
            sbs.AbandonOnTransient = true;
            sbs.MaxDeliveryCount = 3;

            await sbs.ReceiveAsync(message, actions, (_, _) => throw new TransientException("Retry again please."));

            actions.AssertRenew(0);
            actions.AssertAbandon();

            Assert.IsNotNull(actions.PropertiesModified);
            Assert.AreEqual(actions.PropertiesModified!["SubscriberAbandonReason"], "Retry again please.");
        }

        [Test]
        public async Task MaxDeliveryCount_EqualTo()
        {
            using var test = FunctionTester.Create<Startup>();
            var actions = test.CreateServiceBusMessageActions();
            var message = test.CreateServiceBusMessage(new { id = "A", name = "B", price = 1.99m }, m => m.Header.DeliveryCount = 3);

            var sbs = test.Services.GetRequiredService<ServiceBusSubscriber>();
            sbs.AbandonOnTransient = true;
            sbs.MaxDeliveryCount = 3;

            await sbs.ReceiveAsync(message, actions, (_, _) => throw new TransientException("Retry again please."));

            actions.AssertRenew(0);
            actions.AssertDeadLetter("MaxDeliveryCountExceeded", "Message could not be consumed after 3 attempts (as defined by ServiceBusSubscriber).");
            Assert.IsNotNull(actions.PropertiesModified);
            Assert.IsNotNull(actions.PropertiesModified!["SubscriberException"]);
        }

        [Test]
        public async Task Complete()
        {
            using var test = FunctionTester.Create<Startup>();
            var actions = test.CreateServiceBusMessageActions();
            var message = test.CreateServiceBusMessage(new { id = "A", name = "B", price = 1.99m }, m => m.Header.DeliveryCount = 3);

            var sbs = test.Services.GetRequiredService<ServiceBusSubscriber>();
            sbs.AbandonOnTransient = true;

            await sbs.ReceiveAsync(message, actions, (_, _) => Task.CompletedTask);

            actions.AssertRenew(0);
            actions.AssertComplete();
        }

        [Test]
        public async Task Unhandled_DeadLetter()
        {
            using var test = FunctionTester.Create<Startup>();
            var actions = test.CreateServiceBusMessageActions();
            var message = test.CreateServiceBusMessage(new { id = "A", name = "B", price = 1.99m });

            var sbs = test.Services.GetRequiredService<ServiceBusSubscriber>();
            sbs.AbandonOnTransient = true;

            await sbs.ReceiveAsync(message, actions, (_, _) => throw new DivideByZeroException("Zero is bad dude!"));

            actions.AssertRenew(0);
            actions.AssertDeadLetter("UnhandledError", "Zero is bad dude!");
        }
    }
}