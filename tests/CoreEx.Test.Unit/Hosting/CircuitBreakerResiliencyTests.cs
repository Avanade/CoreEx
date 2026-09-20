using CoreEx.Hosting;
using CoreEx.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Polly;

namespace CoreEx.Test.Unit.Hosting;

[TestFixture]
public class CircuitBreakerResiliencyTests
{
    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (!condition())
        {
            if (sw.Elapsed > timeout)
                throw new TimeoutException("Condition was not met within the timeout.");

            await Task.Delay(10);
        }
    }

    private static ResilienceContext CreateContext(TestOwner owner)
    {
        var ctx = ResilienceContextPool.Shared.Get();
        ctx.Properties.Set(ResilienceOwner<TestOwner>.PropertyKey, owner);
        return ctx;
    }

    [Test]
    public async Task Create_TripsAfterFailures_PausesThenSelfResumes()
    {
        var owner = new TestOwner();
        var pipeline = CircuitBreakerResiliency<TestOwner>.Create("Test owner", o => o.Logger, (o, pause, ct) => o.PauseAsync(pause), (o, ct) => o.ResumeAsync(),
            minimumThroughput: 2, samplingDuration: TimeSpan.FromSeconds(10), breakDuration: TimeSpan.FromMilliseconds(50), maxBreakDuration: TimeSpan.FromMilliseconds(200));

        var ctx = CreateContext(owner);
        try
        {
            // Two failures within the sampling window, at 100% failure ratio, is enough to trip the breaker (default failureRatio is 0.1).
            (await pipeline.ExecuteAsync(async _ => Result.Fail(new InvalidOperationException("boom")), ctx)).IsFailure.Should().BeTrue();
            (await pipeline.ExecuteAsync(async _ => Result.Fail(new InvalidOperationException("boom")), ctx)).IsFailure.Should().BeTrue();

            await WaitUntilAsync(() => owner.PauseDurations.Count > 0, TimeSpan.FromSeconds(2));
            owner.PauseDurations.Should().ContainSingle();

            // The breaker's own scheduled pause/delay/resume should self-resume once the (short) break duration elapses.
            await WaitUntilAsync(() => owner.ResumeCount > 0, TimeSpan.FromSeconds(2));
            owner.ResumeCount.Should().Be(1);
        }
        finally
        {
            ResilienceContextPool.Shared.Return(ctx);
        }
    }

    [Test]
    public async Task Create_ShouldHandleExcludesError_NeverTrips()
    {
        var owner = new TestOwner();
        var pipeline = CircuitBreakerResiliency<TestOwner>.Create("Test owner", o => o.Logger, (o, pause, ct) => o.PauseAsync(pause), (o, ct) => o.ResumeAsync(),
            shouldHandle: r => r.Error is not ExcludedException,
            minimumThroughput: 2, samplingDuration: TimeSpan.FromSeconds(10), breakDuration: TimeSpan.FromMilliseconds(50));

        var ctx = CreateContext(owner);
        try
        {
            for (var i = 0; i < 10; i++)
                (await pipeline.ExecuteAsync(async _ => Result.Fail(new ExcludedException()), ctx)).IsFailure.Should().BeTrue();

            // Give any (unexpected) fire-and-forget pause a moment to have shown up were it going to.
            await Task.Delay(100);
            owner.PauseDurations.Should().BeEmpty();
        }
        finally
        {
            ResilienceContextPool.Shared.Return(ctx);
        }
    }

    [Test]
    public async Task Create_PauseAsyncThrows_IsCaughtAndLogged_DoesNotThrowFromPipeline()
    {
        var owner = new TestOwner { ThrowOnPause = true };
        var pipeline = CircuitBreakerResiliency<TestOwner>.Create("Test owner", o => o.Logger, (o, pause, ct) => o.PauseAsync(pause), (o, ct) => o.ResumeAsync(),
            minimumThroughput: 2, samplingDuration: TimeSpan.FromSeconds(10), breakDuration: TimeSpan.FromMilliseconds(20));

        var ctx = CreateContext(owner);
        try
        {
            // Tripping the breaker schedules a fire-and-forget pause/delay/resume; a failure inside that must be swallowed (logged), never surfaced as an unobserved task exception.
            (await pipeline.ExecuteAsync(async _ => Result.Fail(new InvalidOperationException("boom")), ctx)).IsFailure.Should().BeTrue();
            (await pipeline.ExecuteAsync(async _ => Result.Fail(new InvalidOperationException("boom")), ctx)).IsFailure.Should().BeTrue();

            await WaitUntilAsync(() => owner.PauseAttempted, TimeSpan.FromSeconds(2));

            // Resume must never be reached since pause itself threw.
            await Task.Delay(200);
            owner.ResumeCount.Should().Be(0);
        }
        finally
        {
            ResilienceContextPool.Shared.Return(ctx);
        }
    }

    private sealed class ExcludedException : Exception;

    private sealed class TestOwner
    {
        public ILogger Logger { get; } = NullLogger.Instance;

        public List<TimeSpan> PauseDurations { get; } = [];

        public int ResumeCount { get; private set; }

        public bool ThrowOnPause { get; set; }

        public bool PauseAttempted { get; private set; }

        public Task PauseAsync(TimeSpan pause)
        {
            PauseAttempted = true;
            if (ThrowOnPause)
                throw new InvalidOperationException("Pause failed.");

            PauseDurations.Add(pause);
            return Task.CompletedTask;
        }

        public Task ResumeAsync()
        {
            ResumeCount++;
            return Task.CompletedTask;
        }
    }
}
