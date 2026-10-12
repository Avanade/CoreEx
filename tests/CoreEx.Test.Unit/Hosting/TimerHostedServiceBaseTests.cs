using CoreEx.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;

namespace CoreEx.Test.Unit.Hosting;

[TestFixture]
public class TimerHostedServiceBaseTests
{
    private static ServiceProvider CreateServiceProvider(IConfiguration? configuration = null)
    {
        var sc = new ServiceCollection();
        sc.AddSingleton(configuration ?? new ConfigurationBuilder().Build());
        sc.AddScoped<ExecutionContext>();
        return sc.BuildServiceProvider();
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        while (!condition())
        {
            if (sw.Elapsed > timeout)
                throw new TimeoutException("Condition was not met within the timeout.");

            await Task.Delay(10);
        }
    }

    [Test]
    public void ArePauseAndResumeSupported_DefaultsToTrue()
    {
        using var sp = CreateServiceProvider();
        var svc = new TestTimerService(sp, NullLogger.Instance);
        svc.ArePauseAndResumeSupported.Should().BeTrue();
    }

    [Test]
    public async Task StartAsync_ReadsIntervalSettingsFromConfiguration()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "CoreEx:Host:Services:TestTimerService:Interval", "00:00:05" },
            { "CoreEx:Host:Services:TestTimerService:FirstInterval", "00:00:01" },
            { "CoreEx:Host:Services:TestTimerService:OnUnhandledInterval", "00:00:02" },
            { "CoreEx:Host:Services:TestTimerService:MaxConsecutiveExecutions", "42" },
            { "CoreEx:Host:Services:TestTimerService:PauseOnUnhandledException", "false" }
        }).Build();

        using var sp = CreateServiceProvider(config);
        var svc = new TestTimerService(sp, NullLogger.Instance);

        await svc.StartAsync(CancellationToken.None);
        try
        {
            svc.Interval.Should().Be(TimeSpan.FromSeconds(5));
            svc.FirstInterval.Should().Be(TimeSpan.FromSeconds(1));
            svc.OnUnhandledInterval.Should().Be(TimeSpan.FromSeconds(2));
            svc.MaxConsecutiveExecutions.Should().Be(42);
            svc.PauseOnUnhandledException.Should().BeFalse();
        }
        finally
        {
            await svc.StopAsync(CancellationToken.None);
        }
    }

    [Test]
    public async Task OnExecuteAsync_IsInvokedByBackgroundLoop()
    {
        using var sp = CreateServiceProvider();
        var svc = new TestTimerService(sp, NullLogger.Instance) { Interval = TimeSpan.FromMilliseconds(20), FirstInterval = TimeSpan.FromMilliseconds(5) };

        await svc.StartAsync(CancellationToken.None);
        try
        {
            await WaitUntilAsync(() => svc.ExecuteCount > 0, TimeSpan.FromSeconds(5));
            svc.ExecuteCount.Should().BeGreaterThan(0);
        }
        finally
        {
            await svc.StopAsync(CancellationToken.None);
        }
    }

    [Test]
    public async Task UnhandledException_WithPauseOnUnhandledException_PausesService()
    {
        using var sp = CreateServiceProvider();
        var svc = new TestTimerService(sp, NullLogger.Instance)
        {
            Interval = TimeSpan.FromMilliseconds(20),
            FirstInterval = TimeSpan.FromMilliseconds(5),
            PauseOnUnhandledException = true,
            ThrowOnExecute = true
        };

        await svc.StartAsync(CancellationToken.None);
        try
        {
            await WaitUntilAsync(() => svc.Status == ServiceStatus.Paused, TimeSpan.FromSeconds(5));
            svc.Status.Should().Be(ServiceStatus.Paused);
            svc.LastException.Should().NotBeNull();
        }
        finally
        {
            await svc.StopAsync(CancellationToken.None);
        }
    }

    [Test]
    public async Task UnhandledException_WithoutPauseOnUnhandledException_ContinuesExecuting()
    {
        using var sp = CreateServiceProvider();
        var svc = new TestTimerService(sp, NullLogger.Instance)
        {
            Interval = TimeSpan.FromMilliseconds(20),
            FirstInterval = TimeSpan.FromMilliseconds(5),
            PauseOnUnhandledException = false,
            ThrowOnExecute = true
        };

        await svc.StartAsync(CancellationToken.None);
        try
        {
            // Should keep retrying (not pause) despite every execution throwing.
            await WaitUntilAsync(() => svc.ExecuteCount >= 2, TimeSpan.FromSeconds(5));
            svc.Status.Should().NotBe(ServiceStatus.Paused);
        }
        finally
        {
            await svc.StopAsync(CancellationToken.None);
        }
    }

    [Test]
    public async Task Resume_AfterManyTimedIterations_WakesService()
    {
        using var sp = CreateServiceProvider();
        var svc = new TestTimerService(sp, NullLogger.Instance) { Interval = TimeSpan.FromMilliseconds(5), FirstInterval = TimeSpan.FromMilliseconds(5) };

        await svc.StartAsync(CancellationToken.None);
        try
        {
            // Many timed iterations (delay wins) previously left abandoned semaphore waiters that would swallow subsequent wake-up signals.
            await WaitUntilAsync(() => svc.ExecuteCount >= 20, TimeSpan.FromSeconds(5));

            await svc.PauseAsync(CancellationToken.None);
            await WaitUntilAsync(() => svc.Status == ServiceStatus.Paused, TimeSpan.FromSeconds(5));
            await Task.Delay(50);

            var count = svc.ExecuteCount;
            await svc.ResumeAsync(CancellationToken.None);
            await WaitUntilAsync(() => svc.ExecuteCount > count, TimeSpan.FromSeconds(5));
        }
        finally
        {
            await svc.StopAsync(CancellationToken.None);
        }
    }

    [Test]
    public async Task OneOffTrigger_WhilePaused_WakesService()
    {
        using var sp = CreateServiceProvider();
        var svc = new TestTimerService(sp, NullLogger.Instance) { Interval = TimeSpan.FromMilliseconds(5), FirstInterval = TimeSpan.FromMilliseconds(5), PauseOnUnhandledException = true, ThrowOnExecute = true };

        await svc.StartAsync(CancellationToken.None);
        try
        {
            await WaitUntilAsync(() => svc.Status == ServiceStatus.Paused, TimeSpan.FromSeconds(5));
            svc.ThrowOnExecute = false;

            // Run a number of timed iterations, then pause again so the loop waits on the signal only.
            await svc.ResumeAsync(CancellationToken.None);
            var count = svc.ExecuteCount;
            await WaitUntilAsync(() => svc.ExecuteCount >= count + 20, TimeSpan.FromSeconds(5));
            await svc.PauseAsync(CancellationToken.None);
            await Task.Delay(50);

            count = svc.ExecuteCount;
            await svc.ResumeAsync(CancellationToken.None);
            await WaitUntilAsync(() => svc.ExecuteCount > count, TimeSpan.FromSeconds(5));
        }
        finally
        {
            await svc.StopAsync(CancellationToken.None);
        }
    }

    [Test]
    public async Task StartTriggerStop_Stress_DoesNotThrow()
    {
        using var sp = CreateServiceProvider();

        for (var i = 0; i < 200; i++)
        {
            using var svc = new TestTimerService(sp, NullLogger.Instance) { Interval = TimeSpan.FromMilliseconds(1), FirstInterval = TimeSpan.FromMilliseconds(1) };
            await svc.StartAsync(CancellationToken.None);

            using var cts = new CancellationTokenSource();
            var trigger = Task.Run(() =>
            {
                while (!cts.IsCancellationRequested)
                    svc.OneOffTrigger();
            });

            await Task.Delay(i % 5);
            await svc.StopAsync(CancellationToken.None);
            cts.Cancel();
            await trigger;
        }
    }

    [Test]
    public async Task OneOffTrigger_AfterDispose_DoesNotThrow()
    {
        using var sp = CreateServiceProvider();
        var svc = new TestTimerService(sp, NullLogger.Instance) { Interval = TimeSpan.FromMilliseconds(5), FirstInterval = TimeSpan.FromMilliseconds(5) };

        await svc.StartAsync(CancellationToken.None);
        svc.Dispose();

        svc.Invoking(s => s.OneOffTrigger()).Should().NotThrow();
    }

    private class TestTimerService(IServiceProvider serviceProvider, ILogger logger) : TimerHostedServiceBase(serviceProvider, logger)
    {
        private int _executeCount;

        public int ExecuteCount => _executeCount;

        public bool ThrowOnExecute { get; set; }

        protected override Task<bool> OnExecuteAsync(ExecutionContext executionContext, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _executeCount);

            if (ThrowOnExecute)
                throw new InvalidOperationException("Test failure.");

            return Task.FromResult(false);
        }
    }
}
