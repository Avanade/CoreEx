using CoreEx.Events;
using CoreEx.Events.Publishing;
using Microsoft.Extensions.DependencyInjection;
using UnitTestEx.Expectations;
using UnitTestEx.Generic;

namespace CoreEx.UnitTesting.Test.Unit;

public class EventExpecationsConfigTests
{
    // Each test creates its own GenericTester instance (rather than using the shared WithGenericTester<T> fixture) since EventExpectations state
    // is keyed by request id, which is always null for a non-HTTP GenericTester — a shared tester would leak event-expectation state between tests.
    private static GenericTester<EntryPoint> CreateTest() => GenericTester.Create<EntryPoint>();

    private static void PublishEvent(GenericTester<EntryPoint> test, string entity = "thing", string action = "created")
    {
        using var scope = test.Services.CreateScope();
        var ep = scope.ServiceProvider.GetRequiredKeyedService<IEventPublisher>(EntryPoint.EventsServiceKey);
        ep.Add("dest", EventData.CreateEvent(entity, action).WithPartitionKey());
        ep.PublishAsync().GetAwaiter().GetResult();
    }

    [Test]
    public void ExpectEvents_AssertorThenCount_Throws()
    {
        var test = CreateTest();
        Action act = () => test.ExpectEvents(EntryPoint.EventsServiceKey, cfg => cfg.AssertWithValue("dest", "title").AssertCount(1));

        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void ExpectEvents_MultipleAssertors_StillWorks()
    {
        var test = CreateTest();
        test.ExpectEvents(EntryPoint.EventsServiceKey, cfg => cfg.AssertMetadata("dest").AssertMetadata("dest"))
            .Run(() =>
            {
                using var scope = test.Services.CreateScope();
                var ep = scope.ServiceProvider.GetRequiredKeyedService<IEventPublisher>(EntryPoint.EventsServiceKey);
                ep.Add("dest", EventData.CreateEvent("thing", "created").WithPartitionKey());
                ep.Add("dest", EventData.CreateEvent("thing", "updated").WithPartitionKey());
                ep.PublishAsync().GetAwaiter().GetResult();
            })
            .AssertSuccess();
    }

    [Test]
    public void AssertCloudEvent_IgnoresDefaultPaths()
    {
        var test = CreateTest();
        var formatter = test.Services.GetRequiredService<IEventFormatter>();

        // Built independently of the actual publish below, so id/time/partitionkey will differ — must still pass, since those are default-ignored paths.
        var expected = formatter.ConvertToCloudEvent(formatter.Format(EventData.CreateEvent("thing", "created").WithPartitionKey()));

        test.ExpectEvents(EntryPoint.EventsServiceKey, cfg => cfg.AssertCloudEvent("dest", expected))
            .Run(() => PublishEvent(test))
            .AssertSuccess();
    }

    [Test]
    public void AssertCustom_IgnoresDefaultPaths()
    {
        var test = CreateTest();
        var formatter = test.Services.GetRequiredService<IEventFormatter>();
        var expected = formatter.ConvertToCloudEvent(formatter.Format(EventData.CreateEvent("thing", "created").WithPartitionKey()));

        test.ExpectEvents(EntryPoint.EventsServiceKey, cfg => cfg.AssertCustom("dest", (assertor, args, actual) => assertor.AssertCloudEvent(expected, actual.Event!)))
            .Run(() => PublishEvent(test))
            .AssertSuccess();
    }

    // AssertWithValue<TValue>(Func<TValue> valueFactory, ...) is for when the expected event payload is not (and cannot be) derived from the
    // tester's own asserted value (AssertArgs.Value) - e.g. a host-less GenericTester (no IValueExpectations<TValue> at all, so args.Value is
    // always default), or an operation whose published event payload legitimately differs from what it returns. The parameterless AssertWithValue
    // overload only ever compares against args.Value, so it cannot express either scenario.
    public class Widget
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    [Test]
    public void AssertWithValue_WithFactory_Succeeds_WhenPublishedValueMatchesFactory()
    {
        var test = CreateTest();

        test.ExpectEvents(EntryPoint.EventsServiceKey, cfg => cfg.AssertWithValue(() => new Widget { Id = 42, Name = "Widget-042" }, "dest", "widget.created", updater: ed => ed.WithPartitionKey()))
            .Run(() =>
            {
                using var scope = test.Services.CreateScope();
                var ep = scope.ServiceProvider.GetRequiredKeyedService<IEventPublisher>(EntryPoint.EventsServiceKey);
                ep.Add("dest", new EventData { Title = "widget.created" }.WithValue(new Widget { Id = 42, Name = "Widget-042" }).WithPartitionKey());
                ep.PublishAsync().GetAwaiter().GetResult();
            })
            .AssertSuccess();
    }

    [Test]
    public void AssertWithValue_WithFactory_Fails_WhenPublishedValueDoesNotMatchFactory()
    {
        var test = CreateTest();

        Assert.Throws<AssertionException>(() => test.ExpectEvents(EntryPoint.EventsServiceKey, cfg => cfg.AssertWithValue(() => new Widget { Id = 42, Name = "Widget-042" }, "dest", "widget.created", updater: ed => ed.WithPartitionKey()))
            .Run(() =>
            {
                using var scope = test.Services.CreateScope();
                var ep = scope.ServiceProvider.GetRequiredKeyedService<IEventPublisher>(EntryPoint.EventsServiceKey);
                ep.Add("dest", new EventData { Title = "widget.created" }.WithValue(new Widget { Id = 999, Name = "Different" }).WithPartitionKey());
                ep.PublishAsync().GetAwaiter().GetResult();
            })
            .AssertSuccess());
    }
}
