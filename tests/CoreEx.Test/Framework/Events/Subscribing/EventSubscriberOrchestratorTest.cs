using CoreEx.Configuration;
using CoreEx.Events;
using CoreEx.Events.Subscribing;
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
            var esex = Assert.Throws<EventSubscriberException>(() => eso.TryMatchSubscriber(ees, new EventData { Subject = "my.hr.employee", Type = "blah.blah", Action = "created" }, new EventSubscriberArgs(), out var subscriber, out var valueType));
            Assert.That(esex!.ExceptionSource, Is.EqualTo(EventSubscriberExceptionSource.OrchestratorNotSubscribed));

            eso = new EventSubscriberOrchestrator() { NotSubscribedHandling = ErrorHandling.CompleteAsSilent };
            Assert.IsFalse(eso.TryMatchSubscriber(ees, new EventData { Subject = "my.hr.employee", Type = "blah.blah", Action = "created" }, new EventSubscriberArgs(), out var subscriber, out var valueType));
            Assert.IsNull(subscriber);
            Assert.IsNull(valueType);
        }

        [Test]
        public void NoMatch_WithSubscribers()
        {
            var sb = SetUpServiceProvider();
            var ees = sb.GetRequiredService<EmployeeEventSub>();

            var eso = new EventSubscriberOrchestrator().AddSubscriber<OthersSubscriber>().AddSubscriber<DeleteSubscriber>();
            var esex = Assert.Throws<EventSubscriberException>(() => eso.TryMatchSubscriber(ees, new EventData { Subject = "my.hr.employee", Type = "blah.blah", Action = "created" }, new EventSubscriberArgs(), out var subscriber, out var valueType));
            Assert.That(esex!.ExceptionSource, Is.EqualTo(EventSubscriberExceptionSource.OrchestratorNotSubscribed));

            eso = new EventSubscriberOrchestrator() { NotSubscribedHandling = ErrorHandling.CompleteAsSilent };
            Assert.IsFalse(eso.TryMatchSubscriber(ees, new EventData { Subject = "my.hr.employee", Type = "blah.blah", Action = "created" }, new EventSubscriberArgs(), out var subscriber, out var valueType));
            Assert.IsNull(subscriber);
            Assert.IsNull(valueType);
        }

        [Test]
        public void Match_Success_WithValue()
        {
            var sb = SetUpServiceProvider(sc => sc.AddScoped<ModifySubscriber>());
            var ees = sb.GetRequiredService<EmployeeEventSub>();

            var eso = new EventSubscriberOrchestrator().AddSubscriber<ModifySubscriber>().AddSubscriber<DeleteSubscriber>();
            Assert.IsTrue(eso.TryMatchSubscriber(ees, new EventData { Subject = "my.hr.employee", Type = "blah.blah", Action = "tweaked" }, new EventSubscriberArgs(), out var subscriber, out var valueType));
            Assert.That(subscriber, Is.Not.Null.And.TypeOf<ModifySubscriber>());
            Assert.That(valueType, Is.Not.Null.And.EqualTo(typeof(Employee)));
        }

        [Test]
        public void Match_Success_NoValue()
        {
            var sb = SetUpServiceProvider(sc => sc.AddScoped<DeleteSubscriber>());
            var ees = sb.GetRequiredService<EmployeeEventSub>();

            var eso = new EventSubscriberOrchestrator().AddSubscriber<ModifySubscriber>().AddSubscriber<DeleteSubscriber>();
            Assert.IsTrue(eso.TryMatchSubscriber(ees, new EventData { Subject = "my.hr.employee", Type = "blah.blah", Action = "deleted" }, new EventSubscriberArgs(), out var subscriber, out var valueType));
            Assert.That(subscriber, Is.Not.Null.And.TypeOf<DeleteSubscriber>());
            Assert.That(valueType, Is.Null);
        }

        [Test]
        public void NoMatch_Ambiquous()
        {
            var sb = SetUpServiceProvider(sc => sc.AddScoped<DeleteSubscriber>());
            var ees = sb.GetRequiredService<EmployeeEventSub>();

            var eso = new EventSubscriberOrchestrator().AddSubscriber<DeleteSubscriber>().AddSubscriber<DuplicateSubscriber>().UseAmbiquousSubscriberHandling(ErrorHandling.None);
            var esex = Assert.Throws<EventSubscriberException>(() => eso.TryMatchSubscriber(ees, new EventData { Subject = "my.hr.employee", Type = "blah.blah", Action = "deleted" }, new EventSubscriberArgs(), out var subscriber, out var valueType));
            Assert.That(esex!.ExceptionSource, Is.EqualTo(EventSubscriberExceptionSource.OrchestratorAmbiquousSubscriber));

            eso = new EventSubscriberOrchestrator() { AmbiquousSubscriberHandling = ErrorHandling.CompleteAsSilent }.AddSubscriber<DeleteSubscriber>().AddSubscriber<DuplicateSubscriber>();
            Assert.IsFalse(eso.TryMatchSubscriber(ees, new EventData { Subject = "my.hr.employee", Type = "blah.blah", Action = "deleted" }, new EventSubscriberArgs(), out var subscriber, out var valueType));
            Assert.That(subscriber, Is.Null);
            Assert.That(valueType, Is.Null);
        }

        [Test]
        public void Match_With_ExtendedMatchMethod()
        {
            var sb = SetUpServiceProvider(sc => sc.AddScoped<OthersSubscriber>());
            var ees = sb.GetRequiredService<EmployeeEventSub>();

            var eso = new EventSubscriberOrchestrator().AddSubscriber<OthersSubscriber>();
            var esex = Assert.Throws<EventSubscriberException>(() => eso.TryMatchSubscriber(ees, new EventData { Subject = "my.hr.other", Type = "blah.blah", Action = "created", Key = "XXX" }, new EventSubscriberArgs(), out var subscriber, out var valueType));
            Assert.That(esex!.ExceptionSource, Is.EqualTo(EventSubscriberExceptionSource.OrchestratorNotSubscribed));

            Assert.IsTrue(eso.TryMatchSubscriber(ees, new EventData { Subject = "my.hr.other", Type = "blah.blah", Action = "created", Key = "KEY" }, new EventSubscriberArgs(), out var subscriber2, out var valueType2));
            Assert.That(subscriber2, Is.Not.Null.And.TypeOf<OthersSubscriber>());
            Assert.That(valueType2, Is.Null);
        }

        [Test] public async Task Receive_Unhandled_None() => await ReceiveTest(null, () => throw new System.NotImplementedException("Unhandled exception."), typeof(System.NotImplementedException), false, message: "Unhandled exception.");
        [Test] public async Task Receive_Unhandled_Exception() => await ReceiveTest(ms => ms._UnhandledHandling = ErrorHandling.ThrowSubscriberException, () => throw new System.NotImplementedException("Unhandled exception."), typeof(System.NotImplementedException), true, message: "Unhandled exception.", ins: "Test.Error.UnhandledError");
        [Test] public async Task Receive_Unhandled_CompleteSilent() => await ReceiveTest(ms => ms._UnhandledHandling = ErrorHandling.CompleteAsSilent, () => throw new System.NotImplementedException("Unhandled exception."), ins: "Test.Complete.UnhandledError");

        [Test] public async Task Receive_Unhandled_FailFast()
        {
            // The following must be tested independently (comment out Assert.Inconclusive) to verify as it will kill the test process.
            Assert.Inconclusive("FailFast can not be executed within testing as it will kill the test process; must be manually tested!");
            await ReceiveTest(ms => ms._UnhandledHandling = ErrorHandling.CriticalFailFast, () => throw new System.NotImplementedException("Unhandled exception."));
            Assert.Fail("Should never, ever, ever, get here ;-)");
        }

        [Test] public async Task Receive_Security_None() => await ReceiveTest(null, () => throw new AuthenticationException(), typeof(AuthenticationException), false);
        [Test] public async Task Receive_Security_Exception() => await ReceiveTest(ms => ms._SecurityHandling = ErrorHandling.ThrowSubscriberException, () => throw new AuthorizationException(), typeof(AuthorizationException), true, ins: "Test.Error.AuthorizationError");
        [Test] public async Task Receive_Security_Retry() => await ReceiveTest(ms => ms._SecurityHandling = ErrorHandling.TransientRetry, () => throw new AuthorizationException(), typeof(AuthorizationException), true, true, ins: "Test.Transient.AuthorizationError");
        [Test] public async Task Receive_Security_CompleteSilent() => await ReceiveTest(ms => ms._SecurityHandling = ErrorHandling.CompleteAsSilent, () => throw new AuthorizationException(), ins: "Test.Complete.AuthorizationError");

        [Test] public async Task Receive_InvalidData_None() => await ReceiveTest(null, () => throw new BusinessException(), typeof(BusinessException), false);
        [Test] public async Task Receive_InvalidData_Exception() => await ReceiveTest(ms => ms._InvalidDataHandling = ErrorHandling.ThrowSubscriberException, () => throw new ConflictException(), typeof(ConflictException), true, ins: "Test.Error.ConflictError");
        [Test] public async Task Receive_InvalidData_CompleteSilent() => await ReceiveTest(ms => ms._InvalidDataHandling = ErrorHandling.CompleteAsSilent, () => throw new DuplicateException(), ins: "Test.Complete.DuplicateError");
        [Test] public async Task Receive_InvalidData_Exception_ValueIsRequired() => await ReceiveTest(ms => ms._InvalidDataHandling = ErrorHandling.ThrowSubscriberException, () => throw new DivideByZeroException(), typeof(ValidationException), true, message: "Invalid message; body was not provided, contained invalid JSON, or was incorrectly formatted: Value is required.", ed: new EventData<Employee>(), ins: "Test.Error.ValidationError");

        [Test] public async Task Receive_Concurrency_None() => await ReceiveTest(null, () => throw new ConcurrencyException(), typeof(ConcurrencyException), false);
        [Test] public async Task Receive_Concurrency_Exception() => await ReceiveTest(ms => ms._ConcurrencyHandling = ErrorHandling.ThrowSubscriberException, () => throw new ConcurrencyException(), typeof(ConcurrencyException), true, ins: "Test.Error.ConcurrencyError");
        [Test] public async Task Receive_Concurrency_CompleteSilent() => await ReceiveTest(ms => ms._ConcurrencyHandling = ErrorHandling.CompleteAsSilent, () => throw new ConcurrencyException(), ins: "Test.Complete.ConcurrencyError");

        [Test] public async Task Receive_NotFound_None() => await ReceiveTest(null, () => throw new NotFoundException(), typeof(NotFoundException), false);
        [Test] public async Task Receive_NotFound_Exception() => await ReceiveTest(ms => ms._NotFoundHandling = ErrorHandling.ThrowSubscriberException, () => throw new NotFoundException(), typeof(NotFoundException), true, ins: "Test.Error.NotFoundError");
        [Test] public async Task Receive_NotFound_CompleteSilent() => await ReceiveTest(ms => ms._NotFoundHandling = ErrorHandling.CompleteAsSilent, () => throw new NotFoundException(), ins: "Test.Complete.NotFoundError");

        [Test] public async Task Receive_Transient_None() => await ReceiveTest(null, () => throw new TransientException(), typeof(TransientException), false, true);
        [Test] public async Task Receive_Transient_Retry() => await ReceiveTest(ms => ms._TransientHandling = ErrorHandling.TransientRetry, () => throw new TransientException(), typeof(TransientException), true, true, ins: "Test.Transient.TransientError");
        [Test] public async Task Receive_Transient_Exception() => await ReceiveTest(ms => ms._TransientHandling = ErrorHandling.ThrowSubscriberException, () => throw new TransientException(), typeof(TransientException), true, false, ins: "Test.Error.TransientError");
        [Test] public async Task Receive_Transient_CompleteSilent() => await ReceiveTest(ms => ms._TransientHandling = ErrorHandling.CompleteAsSilent, () => throw new TransientException(), ins: "Test.Complete.TransientError");

        [Test] public async Task Receive_DataConsistency_None() => await ReceiveTest(null, () => throw new DataConsistencyException(), typeof(DataConsistencyException), false, true);
        [Test] public async Task Receive_DataConsistency_Retry() => await ReceiveTest(ms => ms._DataConsistencyHandling = ErrorHandling.TransientRetry, () => throw new DataConsistencyException(), typeof(DataConsistencyException), true, true, ins: "Test.Transient.DataConsistencyError");
        [Test] public async Task Receive_DataConsistency_Exception() => await ReceiveTest(ms => ms._DataConsistencyHandling = ErrorHandling.ThrowSubscriberException, () => throw new DataConsistencyException(), typeof(DataConsistencyException), true, false, ins: "Test.Error.DataConsistencyError");
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
            sc.AddLogging();
            sc.AddCloudEventSerializer();
            sc.AddExecutionContext();
            sc.AddDefaultSettings();
            sc.AddSingleton<EmployeeEventSub>();
            sc.AddEventDataFormatter();
            sc.AddAzureServiceBusReceivedMessageConverter();

            action?.Invoke(sc);

            return sc.BuildServiceProvider();
        }

        private static async Task ReceiveTest(System.Action<ModifySubscriber>? setAction, System.Action receiveAction, System.Type? exceptionType = null, bool eventSubscriberException = true, bool isTransient = false, string? message = null, EventData<Employee>? ed = null, string? ins = null)
        {
            var ms = new ModifySubscriber();
            var sb = SetUpServiceProvider(sc => sc.AddSingleton(ms));

            var eso = new EventSubscriberOrchestrator().AddSubscriber<ModifySubscriber>().AddSubscriber<DeleteSubscriber>();
            var ees = sb.GetRequiredService<EmployeeEventSub>();

            Assert.IsTrue(eso.TryMatchSubscriber(ees, new EventData { Subject = "my.hr.employee", Type = "blah.blah", Action = "tweaked" }, new EventSubscriberArgs(), out var subscriber, out var _));

            setAction?.Invoke(ms);
            ms.Action = () => receiveAction();

            try
            {
                await eso.ReceiveAsync(ees, subscriber!, ed ?? new EventData<Employee> { Value = new Employee { Id = 1, Name = "Bob" } }, new EventSubscriberArgs());
                Assert.IsNull(exceptionType, "Expected an exception!");
            }
            catch (EventSubscriberException esex)
            {
                Assert.IsTrue(eventSubscriberException, "Should be an EventSubscriberException!");
                Assert.That(esex.IsTransient, Is.EqualTo(isTransient));
                Assert.That(esex.InnerException, Is.Not.Null.And.TypeOf(exceptionType));
                if (message != null)
                    Assert.That(esex.Message, Is.Not.Null.And.EqualTo(message));
            }
            catch (System.Exception ex)
            {
                Assert.IsFalse(eventSubscriberException, "Should not be an EventSubscriberException!");
                Assert.That(ex, Is.Not.Null.And.TypeOf(exceptionType));
                if (message != null)
                    Assert.That(ex.Message, Is.Not.Null.And.EqualTo(message));
            }
            finally
            {
                // Reset.
                ms._UnhandledHandling = ErrorHandling.None;
                ms._SecurityHandling = ErrorHandling.None;
                ms._TransientHandling = ErrorHandling.None;
                ms._NotFoundHandling = ErrorHandling.None;
                ms._ConcurrencyHandling = ErrorHandling.None;
                ms._DataConsistencyHandling = ErrorHandling.None;
                ms._InvalidDataHandling = ErrorHandling.None;
            }

            var status = ((TestInstrumentation)ees.Instrumentation!).Status;
            if (ins is null)
                Assert.IsNull(status);
            else
                Assert.AreEqual(ins, status);
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

            public ErrorHandling _UnhandledHandling = ErrorHandling.None;
            public ErrorHandling _SecurityHandling = ErrorHandling.None;
            public ErrorHandling _TransientHandling = ErrorHandling.None;
            public ErrorHandling _NotFoundHandling = ErrorHandling.None;
            public ErrorHandling _ConcurrencyHandling = ErrorHandling.None;
            public ErrorHandling _DataConsistencyHandling = ErrorHandling.None;
            public ErrorHandling _InvalidDataHandling = ErrorHandling.None;

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