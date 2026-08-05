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
