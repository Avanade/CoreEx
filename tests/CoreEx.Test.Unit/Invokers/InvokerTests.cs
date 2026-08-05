using CoreEx.Invokers;

namespace CoreEx.Test.Unit.Invokers;

[TestFixture]
public class InvokerTests
{
    [Test]
    public void Default_HasLoggingAndTracingDisabled()
    {
        Invoker.Default.IsLoggingDisabled.Should().BeTrue();
        Invoker.Default.IsTracingDisabled.Should().BeTrue();
    }

    [Test]
    public void RunSync_Action_ExecutesSynchronously()
    {
        var executed = false;
        Invoker.RunSync(() =>
        {
            executed = true;
            return Task.CompletedTask;
        });

        executed.Should().BeTrue();
    }

    [Test]
    public void RunSync_AlreadyCompletedTask_ReturnsWithoutBlocking()
    {
        var executed = false;
        Invoker.RunSync(() =>
        {
            executed = true;
            return Task.CompletedTask; // Already completed - exercises the fast-path.
        });

        executed.Should().BeTrue();
    }

    [Test]
    public void RunSync_WithResult_ReturnsValue()
    {
        var result = Invoker.RunSync(() => Task.FromResult(42));
        result.Should().Be(42);
    }

    [Test]
    public async Task RunSync_WithAsyncWork_WaitsForCompletionAndReturnsValue()
    {
        var result = Invoker.RunSync(async () =>
        {
            await Task.Delay(10);
            return "done";
        });

        result.Should().Be("done");
        await Task.CompletedTask;
    }

    [Test]
    public void RunSync_PropagatesException()
    {
        Action act = () => Invoker.RunSync(() => throw new InvalidOperationException("boom"));
        act.Should().Throw<InvalidOperationException>().WithMessage("boom");
    }

    [Test]
    public void RunSync_WithResult_PropagatesException()
    {
        Action act = () => Invoker.RunSync<int>(() => throw new InvalidOperationException("boom"));
        act.Should().Throw<InvalidOperationException>().WithMessage("boom");
    }
}
