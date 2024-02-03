using CoreEx.Azure.ServiceBus;
using CoreEx.Events;
using CoreEx.Results;
using CoreEx.TestFunction;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Diagnostics;
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
            var actions = test.CreateWebJobsServiceBusMessageActions();
            var message = test.CreateServiceBusMessageFromValue(new { id = "A", name = "B", price = 1.99m });

            var sbs = test.Services.GetRequiredService<ServiceBusSubscriber>();
            sbs.AbandonOnTransient = false;

            Assert.ThrowsAsync<EventSubscriberException>(() => sbs.ReceiveAsync(message, actions, (_, _) => throw new TransientException("Retry again please.")));

            actions.AssertRenew(0);
            actions.AssertNone();
        }

        [Test]
        public void NoAbandonOnTransient_WithResult()
        {
            using var test = FunctionTester.Create<Startup>();
            var actions = test.CreateWebJobsServiceBusMessageActions();
            var message = test.CreateServiceBusMessageFromValue(new { id = "A", name = "B", price = 1.99m });

            var sbs = test.Services.GetRequiredService<ServiceBusSubscriber>();
            sbs.AbandonOnTransient = false;

            Assert.ThrowsAsync<EventSubscriberException>(() => sbs.ReceiveAsync(message, actions, (_, _) => Task.FromResult(Result.TransientError("Retry again please."))));

            actions.AssertRenew(0);
            actions.AssertNone();
        }

        [Test]
        public async Task AbandonOnTransient()
        {
            using var test = FunctionTester.Create<Startup>();
            var actions = test.CreateWebJobsServiceBusMessageActions();
            var message = test.CreateServiceBusMessageFromValue(new { id = "A", name = "B", price = 1.99m });

            var sbs = test.Services.GetRequiredService<ServiceBusSubscriber>();
            sbs.AbandonOnTransient = true;

            await sbs.ReceiveAsync(message, actions, (_, _) => throw new TransientException("Retry again please."));

            actions.AssertRenew(0);
            actions.AssertAbandon();

            Assert.Multiple(() =>
            {
                Assert.That(actions.PropertiesModified, Is.Not.Null);
                Assert.That(actions.PropertiesModified!["SubscriberAbandonReason"], Is.EqualTo("Retry again please."));
            });
        }

        [Test]
        public async Task AbandonOnTransient_WithResult()
        {
            using var test = FunctionTester.Create<Startup>();
            var actions = test.CreateWebJobsServiceBusMessageActions();
            var message = test.CreateServiceBusMessageFromValue(new { id = "A", name = "B", price = 1.99m });

            var sbs = test.Services.GetRequiredService<ServiceBusSubscriber>();
            sbs.AbandonOnTransient = true;

            await sbs.ReceiveAsync(message, actions, (_, _) => Task.FromResult(Result.TransientError("Retry again please.")));

            actions.AssertRenew(0);
            actions.AssertAbandon();

            Assert.Multiple(() =>
            {
                Assert.That(actions.PropertiesModified, Is.Not.Null);
                Assert.That(actions.PropertiesModified!["SubscriberAbandonReason"], Is.EqualTo("Retry again please."));
            });
        }

        [Test]
        public async Task RetryDelay_DeliveryCount1()
        {
            using var test = FunctionTester.Create<Startup>();
            var actions = test.CreateWebJobsServiceBusMessageActions();
            var message = test.CreateServiceBusMessageFromValue(new { id = "A", name = "B", price = 1.99m });

            var sbs = test.Services.GetRequiredService<ServiceBusSubscriber>();
            sbs.AbandonOnTransient = true;
            sbs.RetryDelay = TimeSpan.FromSeconds(1);

            var sw = Stopwatch.StartNew();
            await sbs.ReceiveAsync(message, actions, (_, _) => throw new TransientException("Retry again please."));
            sw.Stop();

            Assert.That(sw.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(950));

            actions.AssertRenew(0); // Renew is no longer supported; hence 0.
            actions.AssertAbandon();

            Assert.Multiple(() =>
            {
                Assert.That(actions.PropertiesModified, Is.Not.Null);
                Assert.That(actions.PropertiesModified!["SubscriberAbandonReason"], Is.EqualTo("Retry again please."));
            });
        }

        [Test]
        public async Task RetryDelay_DeliveryCount1_WithResult()
        {
            using var test = FunctionTester.Create<Startup>();
            var actions = test.CreateWebJobsServiceBusMessageActions();
            var message = test.CreateServiceBusMessageFromValue(new { id = "A", name = "B", price = 1.99m });

            var sbs = test.Services.GetRequiredService<ServiceBusSubscriber>();
            sbs.AbandonOnTransient = true;
            sbs.RetryDelay = TimeSpan.FromSeconds(1);

            var sw = Stopwatch.StartNew();
            await sbs.ReceiveAsync(message, actions, (_, _) => Task.FromResult(Result.TransientError("Retry again please.")));
            sw.Stop();

            Assert.That(sw.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(950));

            actions.AssertRenew(0); // Renew is no longer supported; hence 0.
            actions.AssertAbandon();

            Assert.Multiple(() =>
            {
                Assert.That(actions.PropertiesModified, Is.Not.Null);
                Assert.That(actions.PropertiesModified!["SubscriberAbandonReason"], Is.EqualTo("Retry again please."));
            });
        }

        [Test]
        public async Task RetryDelay_DeliveryCount2()
        {
            using var test = FunctionTester.Create<Startup>();
            var actions = test.CreateWebJobsServiceBusMessageActions();
            var message = test.CreateServiceBusMessageFromValue(new { id = "A", name = "B", price = 1.99m }, m => m.Header.DeliveryCount = 2);

            var sbs = test.Services.GetRequiredService<ServiceBusSubscriber>();
            sbs.AbandonOnTransient = true;
            sbs.RetryDelay = TimeSpan.FromSeconds(1);

            var sw = Stopwatch.StartNew();
            await sbs.ReceiveAsync(message, actions, (_, _) => throw new TransientException("Retry again please."));
            sw.Stop();

            Assert.That(sw.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(1950));

            actions.AssertRenew(0); // Renew is no longer supported; hence 0.
            actions.AssertAbandon();

            Assert.Multiple(() =>
            {
                Assert.That(actions.PropertiesModified, Is.Not.Null);
                Assert.That(actions.PropertiesModified!["SubscriberAbandonReason"], Is.EqualTo("Retry again please."));
            });
        }

        [Test]
        public async Task RetryDelay_DeliveryCount2_WithMax()
        {
            using var test = FunctionTester.Create<Startup>();
            var actions = test.CreateWebJobsServiceBusMessageActions();
            var message = test.CreateServiceBusMessageFromValue(new { id = "A", name = "B", price = 1.99m }, m => m.Header.DeliveryCount = 2);

            var sbs = test.Services.GetRequiredService<ServiceBusSubscriber>();
            sbs.AbandonOnTransient = true;
            sbs.RetryDelay = TimeSpan.FromSeconds(1);
            sbs.MaxRetryDelay = TimeSpan.FromMilliseconds(1100);

            var sw = Stopwatch.StartNew();
            await sbs.ReceiveAsync(message, actions, (_, _) => throw new TransientException("Retry again please."));
            sw.Stop();

            Assert.That(sw.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(1050));

            actions.AssertRenew(0); // Renew is no longer supported; hence 0.
            actions.AssertAbandon();

            Assert.Multiple(() =>
            {
                Assert.That(actions.PropertiesModified, Is.Not.Null);
                Assert.That(actions.PropertiesModified!["SubscriberAbandonReason"], Is.EqualTo("Retry again please."));
            });
        }

        [Test]
        public async Task RetryDelay_DeliveryCount2_WithMaxOnly()
        {
            using var test = FunctionTester.Create<Startup>();
            var actions = test.CreateWebJobsServiceBusMessageActions();
            var message = test.CreateServiceBusMessageFromValue(new { id = "A", name = "B", price = 1.99m }, m => m.Header.DeliveryCount = 2);

            var sbs = test.Services.GetRequiredService<ServiceBusSubscriber>();
            sbs.AbandonOnTransient = true;
            sbs.MaxRetryDelay = TimeSpan.FromMilliseconds(600);

            var sw = Stopwatch.StartNew();
            await sbs.ReceiveAsync(message, actions, (_, _) => throw new TransientException("Retry again please."));
            sw.Stop();

            Assert.That(sw.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(550));

            actions.AssertRenew(0); // Renew is no longer supported; hence 0.
            actions.AssertAbandon();

            Assert.Multiple(() =>
            {
                Assert.That(actions.PropertiesModified, Is.Not.Null);
                Assert.That(actions.PropertiesModified!["SubscriberAbandonReason"], Is.EqualTo("Retry again please."));
            });
        }

        [Test]
        public async Task MaxDeliveryCount_LessThan()
        {
            using var test = FunctionTester.Create<Startup>();
            var actions = test.CreateWebJobsServiceBusMessageActions();
            var message = test.CreateServiceBusMessageFromValue(new { id = "A", name = "B", price = 1.99m }, m => m.Header.DeliveryCount = 2);

            var sbs = test.Services.GetRequiredService<ServiceBusSubscriber>();
            sbs.AbandonOnTransient = true;
            sbs.MaxDeliveryCount = 3;

            await sbs.ReceiveAsync(message, actions, (_, _) => throw new TransientException("Retry again please."));

            actions.AssertRenew(0);
            actions.AssertAbandon();

            Assert.Multiple(() =>
            {
                Assert.That(actions.PropertiesModified, Is.Not.Null);
                Assert.That(actions.PropertiesModified!["SubscriberAbandonReason"], Is.EqualTo("Retry again please."));
            });
        }

        [Test]
        public async Task MaxDeliveryCount_EqualTo()
        {
            using var test = FunctionTester.Create<Startup>();
            var actions = test.CreateWebJobsServiceBusMessageActions();
            var message = test.CreateServiceBusMessageFromValue(new { id = "A", name = "B", price = 1.99m }, m => m.Header.DeliveryCount = 3);

            var sbs = test.Services.GetRequiredService<ServiceBusSubscriber>();
            sbs.AbandonOnTransient = true;
            sbs.MaxDeliveryCount = 3;

            await sbs.ReceiveAsync(message, actions, (_, _) => throw new TransientException("Retry again please."));

            actions.AssertRenew(0);
            actions.AssertDeadLetter("MaxDeliveryCountExceeded", "Message could not be consumed after 3 attempts (as defined by ServiceBusSubscriber).");
            Assert.That(actions.PropertiesModified, Is.Not.Null);
            Assert.That(actions.PropertiesModified!["SubscriberException"], Is.Not.Null);
        }

        [Test]
        public async Task Complete()
        {
            using var test = FunctionTester.Create<Startup>();
            var actions = test.CreateWebJobsServiceBusMessageActions();
            var message = test.CreateServiceBusMessageFromValue(new { id = "A", name = "B", price = 1.99m }, m => m.Header.DeliveryCount = 3);

            var sbs = test.Services.GetRequiredService<ServiceBusSubscriber>();
            sbs.AbandonOnTransient = true;

            await sbs.ReceiveAsync(message, actions, (_, _) => Task.CompletedTask);

            actions.AssertRenew(0);
            actions.AssertComplete();
        }

        [Test]
        public async Task Complete_Result()
        {
            using var test = FunctionTester.Create<Startup>();
            var actions = test.CreateWebJobsServiceBusMessageActions();
            var message = test.CreateServiceBusMessageFromValue(new { id = "A", name = "B", price = 1.99m }, m => m.Header.DeliveryCount = 3);

            var sbs = test.Services.GetRequiredService<ServiceBusSubscriber>();
            sbs.AbandonOnTransient = true;

            await sbs.ReceiveAsync(message, actions, (_, _) => Result.SuccessTask);

            actions.AssertRenew(0);
            actions.AssertComplete();
        }

        [Test]
        public async Task Unhandled_Throw_DeadLetter()
        {
            using var test = FunctionTester.Create<Startup>();
            var actions = test.CreateWebJobsServiceBusMessageActions();
            var message = test.CreateServiceBusMessageFromValue(new { id = "A", name = "B", price = 1.99m });

            var sbs = test.Services.GetRequiredService<ServiceBusSubscriber>();
            sbs.UnhandledHandling = Events.Subscribing.ErrorHandling.HandleBySubscriber;

            await sbs.ReceiveAsync(message, actions, (_, _) => throw new DivideByZeroException("Zero is bad dude!"));

            actions.AssertRenew(0);
            actions.AssertDeadLetter("UnhandledError", "Zero is bad dude!");
        }

        [Test]
        public async Task Unhandled_None_Bubble()
        {
            using var test = FunctionTester.Create<Startup>();
            var actions = test.CreateWebJobsServiceBusMessageActions();
            var message = test.CreateServiceBusMessageFromValue(new { id = "A", name = "B", price = 1.99m });

            var sbs = test.Services.GetRequiredService<ServiceBusSubscriber>();
            sbs.UnhandledHandling = Events.Subscribing.ErrorHandling.HandleByHost;

            try
            {
                await sbs.ReceiveAsync(message, actions, (_, _) => throw new DivideByZeroException("Zero is bad dude!"));
            }
            catch (DivideByZeroException)
            {
            }
            catch (Exception ex)
            {
                Assert.Fail($"Expected {nameof(DivideByZeroException)} but got {ex.GetType().Name}.");
            }

            actions.AssertRenew(0);
            actions.AssertNone();
        }

        [Test]
        public async Task Unhandled_ContinueAsSilent_Complete()
        {
            using var test = FunctionTester.Create<Startup>();
            var actions = test.CreateWebJobsServiceBusMessageActions();
            var message = test.CreateServiceBusMessageFromValue(new { id = "A", name = "B", price = 1.99m });

            var sbs = test.Services.GetRequiredService<ServiceBusSubscriber>();
            sbs.UnhandledHandling = Events.Subscribing.ErrorHandling.CompleteAsSilent;

            await sbs.ReceiveAsync(message, actions, (_, _) => throw new DivideByZeroException("Zero is bad dude!"));

            actions.AssertRenew(0);
            actions.AssertComplete();
        }
    }
}