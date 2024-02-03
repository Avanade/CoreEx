using CoreEx.Configuration;
using CoreEx.Events;
using CoreEx.Events.Subscribing;
using CoreEx.Hosting.Work;
using CoreEx.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Events.Subscribing
{
    [TestFixture]
    public class EventSubscriberOrchestratorTest
    {
        [Test]
        public void NoMatch_NoSubscribers()
        {
            var sb = SetUpServiceProvider();
            var ees = sb.GetRequiredService<EmployeeEventSub>();

            var eso = new EventSubscriberOrchestrator();
            var match = eso.TryMatchSubscriber(ees, new EventData { Subject = "my.hr.employee", Type = "blah.blah", Action = "created" }, new EventSubscriberArgs());
            Assert.Multiple(() =>
            {
                Assert.That(match.Matched, Is.False);
                Assert.That(match.Subscriber, Is.Null);
                Assert.That(match.ValueType, Is.Null);
            });
        }

        [Test]
        public void NoMatch_WithSubscribers()
        {
            var sb = SetUpServiceProvider();
            var ees = sb.GetRequiredService<EmployeeEventSub>();

            var eso = new EventSubscriberOrchestrator().AddSubscriber<OthersSubscriber>().AddSubscriber<DeleteSubscriber>();
            var match = eso.TryMatchSubscriber(ees, new EventData { Subject = "my.hr.employee", Type = "blah.blah", Action = "created" }, new EventSubscriberArgs());
            Assert.Multiple(() =>
            {
                Assert.That(match.Matched, Is.False);
                Assert.That(match.Subscriber, Is.Null);
                Assert.That(match.ValueType, Is.Null);
            });
        }

        [Test]
        public void Match_Success_WithValue()
        {
            var sb = SetUpServiceProvider(sc => sc.AddScoped<ModifySubscriber>());
            var ees = sb.GetRequiredService<EmployeeEventSub>();

            var eso = new EventSubscriberOrchestrator().AddSubscriber<ModifySubscriber>().AddSubscriber<DeleteSubscriber>();
            var match = eso.TryMatchSubscriber(ees, new EventData { Subject = "my.hr.employee", Type = "blah.blah", Action = "tweaked" }, new EventSubscriberArgs());
            Assert.Multiple(() =>
            {
                Assert.That(match.Matched, Is.True);
                Assert.That(match.Subscriber, Is.Not.Null.And.TypeOf<ModifySubscriber>());
                Assert.That(match.ValueType, Is.Not.Null.And.EqualTo(typeof(Employee)));
            });
        }

        [Test]
        public void Match_Success_NoValue()
        {
            var sb = SetUpServiceProvider(sc => sc.AddScoped<DeleteSubscriber>());
            var ees = sb.GetRequiredService<EmployeeEventSub>();

            var eso = new EventSubscriberOrchestrator().AddSubscriber<ModifySubscriber>().AddSubscriber<DeleteSubscriber>();
            var match = eso.TryMatchSubscriber(ees, new EventData { Subject = "my.hr.employee", Type = "blah.blah", Action = "deleted" }, new EventSubscriberArgs());
            Assert.Multiple(() =>
            {
                Assert.That(match.Matched, Is.True);
                Assert.That(match.Subscriber, Is.Not.Null.And.TypeOf<DeleteSubscriber>());
                Assert.That(match.ValueType, Is.Null);
            });
        }

        [Test]
        public void NoMatch_Ambiquous()
        {
            var sb = SetUpServiceProvider(sc => sc.AddScoped<DeleteSubscriber>());
            var ees = sb.GetRequiredService<EmployeeEventSub>();

            var eso = new EventSubscriberOrchestrator().AddSubscriber<DeleteSubscriber>().AddSubscriber<DuplicateSubscriber>().UseAmbiquousSubscriberHandling(ErrorHandling.HandleByHost);
            var match = eso.TryMatchSubscriber(ees, new EventData { Subject = "my.hr.employee", Type = "blah.blah", Action = "deleted" }, new EventSubscriberArgs());
            Assert.Multiple(() =>
            {
                Assert.That(match.Matched, Is.False);
                Assert.That(match.Subscriber, Is.Not.Null.And.TypeOf<DeleteSubscriber>());
                Assert.That(match.ValueType, Is.Null);
            });
        }

        [Test]
        public void Match_With_ExtendedMatchMethod()
        {
            var sb = SetUpServiceProvider(sc => sc.AddScoped<OthersSubscriber>());
            var ees = sb.GetRequiredService<EmployeeEventSub>();

            var eso = new EventSubscriberOrchestrator().AddSubscriber<OthersSubscriber>();
            var match = eso.TryMatchSubscriber(ees, new EventData { Subject = "my.hr.other", Type = "blah.blah", Action = "created", Key = "KEY" }, new EventSubscriberArgs());
            Assert.Multiple(() =>
            {
                Assert.That(match.Matched, Is.True);
                Assert.That(match.Subscriber, Is.Not.Null.And.TypeOf<OthersSubscriber>());
                Assert.That(match.ValueType, Is.Null);
            });

            match = eso.TryMatchSubscriber(ees, new EventData { Subject = "my.hr.other", Type = "blah.blah", Action = "created", Key = "XXX" }, new EventSubscriberArgs());
            Assert.Multiple(() =>
            {
                Assert.That(match.Matched, Is.False);
                Assert.That(match.Subscriber, Is.Null);
                Assert.That(match.ValueType, Is.Null);
            });
        }

        [Test] public async Task Receive_Unhandled_None() => await ReceiveTest(null, () => throw new System.NotImplementedException("Unhandled exception."), typeof(System.NotImplementedException), false, message: "Unhandled exception.");
        [Test] public async Task Receive_Unhandled_Exception() => await ReceiveTest(ms => ms._UnhandledHandling = ErrorHandling.HandleBySubscriber, () => throw new System.NotImplementedException("Unhandled exception."), typeof(System.NotImplementedException), true, message: "Unhandled exception.", ins: "Test.Error.UnhandledError");
        [Test] public async Task Receive_Unhandled_CompleteSilent() => await ReceiveTest(ms => ms._UnhandledHandling = ErrorHandling.CompleteAsSilent, () => throw new System.NotImplementedException("Unhandled exception."), ins: "Test.Complete.UnhandledError");

        [Test] public async Task Receive_Unhandled_FailFast()
        {
            // The following must be tested independently (comment out Assert.Inconclusive) to verify as it will kill the test process.
            Assert.Inconclusive("FailFast can not be executed within testing as it will kill the test process; must be manually tested!");
            await ReceiveTest(ms => ms._UnhandledHandling = ErrorHandling.CriticalFailFast, () => throw new System.NotImplementedException("Unhandled exception."));
            Assert.Fail("Should never, ever, ever, get here ;-)");
        }

        [Test] public async Task Receive_Security_None() => await ReceiveTest(null, () => throw new AuthenticationException(), typeof(AuthenticationException), false);
        [Test] public async Task Receive_Security_Exception() => await ReceiveTest(ms => ms._SecurityHandling = ErrorHandling.HandleBySubscriber, () => throw new AuthorizationException(), typeof(AuthorizationException), true, ins: "Test.Error.AuthorizationError");
        [Test] public async Task Receive_Security_Retry() => await ReceiveTest(ms => ms._SecurityHandling = ErrorHandling.Retry, () => throw new AuthorizationException(), typeof(AuthorizationException), true, true, ins: "Test.Retry.AuthorizationError");
        [Test] public async Task Receive_Security_CompleteSilent() => await ReceiveTest(ms => ms._SecurityHandling = ErrorHandling.CompleteAsSilent, () => throw new AuthorizationException(), ins: "Test.Complete.AuthorizationError");

        [Test] public async Task Receive_InvalidData_None() => await ReceiveTest(null, () => throw new BusinessException(), typeof(BusinessException), false);
        [Test] public async Task Receive_InvalidData_Exception() => await ReceiveTest(ms => ms._InvalidDataHandling = ErrorHandling.HandleBySubscriber, () => throw new ConflictException(), typeof(ConflictException), true, ins: "Test.Error.ConflictError");
        [Test] public async Task Receive_InvalidData_CompleteSilent() => await ReceiveTest(ms => ms._InvalidDataHandling = ErrorHandling.CompleteAsSilent, () => throw new DuplicateException(), ins: "Test.Complete.DuplicateError");
        [Test] public async Task Receive_InvalidData_Exception_ValueIsRequired() => await ReceiveTest(ms => ms._InvalidDataHandling = ErrorHandling.HandleBySubscriber, () => throw new DivideByZeroException(), typeof(ValidationException), true, message: "Invalid message; body was not provided, contained invalid JSON, or was incorrectly formatted: Value is required.", ed: new EventData<Employee>(), ins: "Test.Error.ValidationError");

        [Test] public async Task Receive_Concurrency_None() => await ReceiveTest(null, () => throw new ConcurrencyException(), typeof(ConcurrencyException), false);
        [Test] public async Task Receive_Concurrency_Exception() => await ReceiveTest(ms => ms._ConcurrencyHandling = ErrorHandling.HandleBySubscriber, () => throw new ConcurrencyException(), typeof(ConcurrencyException), true, ins: "Test.Error.ConcurrencyError");
        [Test] public async Task Receive_Concurrency_CompleteSilent() => await ReceiveTest(ms => ms._ConcurrencyHandling = ErrorHandling.CompleteAsSilent, () => throw new ConcurrencyException(), ins: "Test.Complete.ConcurrencyError");

        [Test] public async Task Receive_NotFound_None() => await ReceiveTest(null, () => throw new NotFoundException(), typeof(NotFoundException), false);
        [Test] public async Task Receive_NotFound_Exception() => await ReceiveTest(ms => ms._NotFoundHandling = ErrorHandling.HandleBySubscriber, () => throw new NotFoundException(), typeof(NotFoundException), true, ins: "Test.Error.NotFoundError");
        [Test] public async Task Receive_NotFound_CompleteSilent() => await ReceiveTest(ms => ms._NotFoundHandling = ErrorHandling.CompleteAsSilent, () => throw new NotFoundException(), ins: "Test.Complete.NotFoundError");

        [Test] public async Task Receive_Transient_None() => await ReceiveTest(null, () => throw new TransientException(), typeof(TransientException), false, true);
        [Test] public async Task Receive_Transient_Retry() => await ReceiveTest(ms => ms._TransientHandling = ErrorHandling.Retry, () => throw new TransientException(), typeof(TransientException), true, true, ins: "Test.Retry.TransientError");
        [Test] public async Task Receive_Transient_Exception() => await ReceiveTest(ms => ms._TransientHandling = ErrorHandling.HandleBySubscriber, () => throw new TransientException(), typeof(TransientException), true, false, ins: "Test.Error.TransientError");
        [Test] public async Task Receive_Transient_CompleteSilent() => await ReceiveTest(ms => ms._TransientHandling = ErrorHandling.CompleteAsSilent, () => throw new TransientException(), ins: "Test.Complete.TransientError");

        [Test] public async Task Receive_DataConsistency_None() => await ReceiveTest(null, () => throw new DataConsistencyException(), typeof(DataConsistencyException), false, true);
        [Test] public async Task Receive_DataConsistency_Retry() => await ReceiveTest(ms => ms._DataConsistencyHandling = ErrorHandling.Retry, () => throw new DataConsistencyException(), typeof(DataConsistencyException), true, true, ins: "Test.Retry.DataConsistencyError");
        [Test] public async Task Receive_DataConsistency_Exception() => await ReceiveTest(ms => ms._DataConsistencyHandling = ErrorHandling.HandleBySubscriber, () => throw new DataConsistencyException(), typeof(DataConsistencyException), true, false, ins: "Test.Error.DataConsistencyError");
        [Test] public async Task Receive_DataConsistency_CompleteSilent() => await ReceiveTest(ms => ms._DataConsistencyHandling = ErrorHandling.CompleteAsSilent, () => throw new DataConsistencyException(), ins: "Test.Complete.DataConsistencyError");

        [Test] public async Task Receive_Success() => await ReceiveTest(null, () => { }, null, false, ins: "Test.Complete.Success");

        [Test]
        public void GetSubscriber()
        {
            var s = EventSubscriberOrchestrator.GetSubscribers<EventSubscriberOrchestratorTest>();
            Assert.That(s, Is.Not.Null.And.Length.EqualTo(4));
        }

        private static ServiceProvider SetUpServiceProvider(Action<IServiceCollection>? action = null)
        {
            var sc = new ServiceCollection();
            sc.AddLogging(lb => lb.AddConsole());
            sc.AddCloudEventSerializer();
            sc.AddExecutionContext();
            sc.AddDefaultSettings();
            sc.AddSingleton<EmployeeEventSub>();
            sc.AddEventDataFormatter();
            sc.AddAzureServiceBusReceivedMessageConverter();
            sc.AddSingleton<IWorkStatePersistence, InMemoryWorkStatePersistence>();
            sc.AddSingleton<WorkStateOrchestrator>();

            action?.Invoke(sc);

            return sc.BuildServiceProvider();
        }

        private static async Task ReceiveTest(System.Action<ModifySubscriber>? setAction, System.Action receiveAction, System.Type? exceptionType = null, bool eventSubscriberException = true, bool isTransient = false, string? message = null, EventData<Employee>? ed = null, string? ins = null)
        {
            var ms = new ModifySubscriber();
            var sb = SetUpServiceProvider(sc => sc.AddSingleton(ms));

            var eso = new EventSubscriberOrchestrator().AddSubscriber<ModifySubscriber>().AddSubscriber<DeleteSubscriber>();
            var ees = sb.GetRequiredService<EmployeeEventSub>();

            var match = eso.TryMatchSubscriber(ees, new EventData { Subject = "my.hr.employee", Type = "blah.blah", Action = "tweaked" }, new EventSubscriberArgs());
            Assert.That(match.Matched, Is.True);

            setAction?.Invoke(ms);
            ms.Action = () => receiveAction();

            try
            {
                await eso.ReceiveAsync(ees, match.Subscriber!, ed ?? new EventData<Employee> { Value = new Employee { Id = 1, Name = "Bob" } }, new EventSubscriberArgs());
                Assert.That(exceptionType, Is.Null, "Expected an exception!");
            }
            catch (EventSubscriberException esex)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(eventSubscriberException, Is.True, "Should be an EventSubscriberException!");
                    Assert.That(esex.IsTransient, Is.EqualTo(isTransient));
                    Assert.That(esex.InnerException, Is.Not.Null.And.TypeOf(exceptionType!));
                });
                if (message != null)
                    Assert.That(esex.Message, Is.Not.Null.And.EqualTo(message));
            }
            catch (System.Exception ex)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(eventSubscriberException, Is.False, "Should not be an EventSubscriberException!");
                    Assert.That(ex, Is.Not.Null.And.TypeOf(exceptionType!));
                });
                if (message != null)
                    Assert.That(ex.Message, Is.Not.Null.And.EqualTo(message));
            }
            finally
            {
                // Reset.
                ms._UnhandledHandling = ErrorHandling.HandleByHost;
                ms._SecurityHandling = ErrorHandling.HandleByHost;
                ms._TransientHandling = ErrorHandling.HandleByHost;
                ms._NotFoundHandling = ErrorHandling.HandleByHost;
                ms._ConcurrencyHandling = ErrorHandling.HandleByHost;
                ms._DataConsistencyHandling = ErrorHandling.HandleByHost;
                ms._InvalidDataHandling = ErrorHandling.HandleByHost;
            }

            var status = ((TestInstrumentation)ees.Instrumentation!).Status;
            if (ins is null)
                Assert.That(status, Is.Null);
            else
                Assert.That(status, Is.EqualTo(ins));
        }

        [EventSubscriber("my.hr.employee", "created", "updated")]
        [EventSubscriber("my.hr.employee", "tweaked")]
        public class ModifySubscriber : SubscriberBase<Employee>
        {
            public System.Action Action { get; set; } = () => throw new System.NotImplementedException("Unhandled exception.");

            public override Task<Result> ReceiveAsync(EventData<Employee> @event, EventSubscriberArgs args, CancellationToken cancellationToken)
            {
                Action();
                return Task.FromResult(Result.Success);
            }

            public ErrorHandling _UnhandledHandling = ErrorHandling.HandleByHost;
            public ErrorHandling _SecurityHandling = ErrorHandling.HandleByHost;
            public ErrorHandling _TransientHandling = ErrorHandling.HandleByHost;
            public ErrorHandling _NotFoundHandling = ErrorHandling.HandleByHost;
            public ErrorHandling _ConcurrencyHandling = ErrorHandling.HandleByHost;
            public ErrorHandling _DataConsistencyHandling = ErrorHandling.HandleByHost;
            public ErrorHandling _InvalidDataHandling = ErrorHandling.HandleByHost;

            public override ErrorHandling UnhandledHandling => _UnhandledHandling;

            public override ErrorHandling SecurityHandling => _SecurityHandling;

            public override ErrorHandling TransientHandling => _TransientHandling;

            public override ErrorHandling NotFoundHandling => _NotFoundHandling;

            public override ErrorHandling ConcurrencyHandling => _ConcurrencyHandling;

            public override ErrorHandling DataConsistencyHandling => _DataConsistencyHandling;

            public override ErrorHandling InvalidDataHandling => _InvalidDataHandling;
        }

        [EventSubscriber("my.hr.employee", "deleted")]
        public class DeleteSubscriber : SubscriberBase
        {
            public override Task<Result> ReceiveAsync(EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken)
            {
                throw new System.NotImplementedException();
            }
        }

        [EventSubscriber("my.hr.employee", "deleted")]
        public class DuplicateSubscriber : SubscriberBase
        {
            public override Task<Result> ReceiveAsync(EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken)
            {
                throw new System.NotImplementedException();
            }
        }

        [EventSubscriber("my.hr.other", ExtendedMatchMethod = nameof(IsExtendedMatch))]
        public class OthersSubscriber : SubscriberBase
        {
            public override Task<Result> ReceiveAsync(EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken)
            {
                throw new System.NotImplementedException();
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Needed by the ExtendedMatchMethod functionality.")]
            public static bool IsExtendedMatch(EventData ed, EventSubscriberArgs args) => ed.Key == "KEY";
        }

        public class Employee
        {
            public int? Id { get; set; }
            public string? Name { get; set; }
        }

        public class EmployeeEventSub : EventSubscriberBase
        {
            public EmployeeEventSub(IEventDataConverter eventDataConverter, ExecutionContext executionContext, SettingsBase settings, ILogger<EventSubscriberBase> logger, EventSubscriberInvoker? eventSubscriberInvoker = null)
                : base(eventDataConverter, executionContext, settings, logger, eventSubscriberInvoker)
            {
                Instrumentation = new TestInstrumentation();
            }
        }

        public class TestInstrumentation : EventSubscriberInstrumentationBase
        {
            public override void Instrument(ErrorHandling? errorHandling = null, Exception? exception = null) => Status += GetInstrumentName("Test", errorHandling, exception);

            public string? Status { get; set; } 
        }
    }
}