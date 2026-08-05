using CoreEx.Invokers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace CoreEx.Test.Unit.Invokers;

[TestFixture]
public class InvokerBaseTests
{
    [Test]
    public void Constructor_SetsTypeAndName()
    {
        var invoker = new TestInvoker();
        invoker.Type.Should().Be(typeof(TestInvoker));
        invoker.Name.Should().Be(InvokerNameAttribute.GetName<TestInvoker>());
    }

    [Test]
    public void Constructor_NoServiceProvider_LoggerAndConfigurationAreNull()
    {
        var invoker = new TestInvoker();
        invoker.Logger.Should().BeNull();
        invoker.Configuration.Should().BeNull();
    }

    [Test]
    public void Constructor_WithServiceProvider_ResolvesConfiguration()
    {
        var sc = new ServiceCollection();
        sc.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        using var sp = sc.BuildServiceProvider();

        var invoker = new TestInvoker(sp);
        invoker.Configuration.Should().NotBeNull();
    }

    [Test]
    public void Defaults_ActivityKindIsInternal_TracingAndLoggingNotDisabled()
    {
        var invoker = new TestInvoker();
        invoker.ActivityKind.Should().Be(ActivityKind.Internal);
        invoker.IsTracingDisabled.Should().BeFalse();
        invoker.IsLoggingDisabled.Should().BeFalse();
    }

    [Test]
    public async Task InvokeAsync_WithResult_ReturnsValue()
    {
        var invoker = new TestInvoker();
        var result = await invoker.InvokeAsync(new object(), async (tracer, ct) => { await Task.Yield(); return 42; });
        result.Should().Be(42);
    }

    [Test]
    public async Task InvokeAsync_NoResult_Executes()
    {
        var invoker = new TestInvoker();
        var executed = false;

        await invoker.InvokeAsync(new object(), async tracer => { await Task.Yield(); executed = true; });

        executed.Should().BeTrue();
    }

    [Test]
    public async Task InvokeAsync_Success_FiresOnActivityStartAndComplete_NotException()
    {
        using var listener = CreateAllDataListener();

        var invoker = new TestInvoker();
        var result = await invoker.InvokeAsync(new object(), async (tracer, ct) => { await Task.Yield(); return 42; });

        result.Should().Be(42);
        invoker.OnActivityStartCalled.Should().BeTrue();
        invoker.OnActivityCompleteCalled.Should().BeTrue();
        invoker.OnActivityExceptionCalled.Should().BeFalse();
    }

    [Test]
    public async Task InvokeAsync_Exception_FiresOnActivityException_AndPropagates()
    {
        using var listener = CreateAllDataListener();

        var invoker = new TestInvoker();
        Func<Task> act = () => invoker.InvokeAsync<int>(new object(), async (tracer, ct) => { await Task.Yield(); throw new InvalidOperationException("boom"); });

        await act.Should().ThrowAsync<InvalidOperationException>();
        invoker.OnActivityExceptionCalled.Should().BeTrue();
        invoker.OnActivityCompleteCalled.Should().BeFalse();
    }

    [Test]
    public async Task InvokeAsyncWithArgs_PassesArgsThrough()
    {
        var invoker = new TestArgsInvoker();
        var result = await invoker.InvokeAsync(new object(), "hello", async (tracer, args, ct) => { await Task.Yield(); return args.Length; });

        result.Should().Be(5);
    }

    private static ActivityListener CreateAllDataListener()
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    private class TestInvoker(IServiceProvider? serviceProvider = null) : InvokerBase<object>(serviceProvider)
    {
        public bool OnActivityStartCalled { get; private set; }
        public bool OnActivityCompleteCalled { get; private set; }
        public bool OnActivityExceptionCalled { get; private set; }

        protected override void OnActivityStart(InvokerTracer tracer)
        {
            OnActivityStartCalled = true;
            base.OnActivityStart(tracer);
        }

        protected override void OnActivityComplete(InvokerTracer tracer)
        {
            OnActivityCompleteCalled = true;
            base.OnActivityComplete(tracer);
        }

        protected override void OnActivityException(InvokerTracer tracer, Exception exception)
        {
            OnActivityExceptionCalled = true;
            base.OnActivityException(tracer, exception);
        }
    }

    private class TestArgsInvoker : InvokerBase<object, string>
    {
    }
}
