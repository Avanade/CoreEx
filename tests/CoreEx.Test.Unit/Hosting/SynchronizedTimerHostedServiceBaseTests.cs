using CoreEx.Hosting;
using CoreEx.Hosting.Synchronization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;

namespace CoreEx.Test.Unit.Hosting;

[TestFixture]
public class SynchronizedTimerHostedServiceBaseTests
{
    private static ServiceProvider CreateServiceProvider(TestSynchronizer synchronizer, IConfiguration? configuration = null)
    {
        var sc = new ServiceCollection();
        sc.AddSingleton(configuration ?? new ConfigurationBuilder().Build());
        sc.AddExecutionContext();
        sc.AddSingleton(synchronizer);
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
    public async Task SynchronizedExecuteAsync_IsInvoked_WhenEnterSucceeds()
    {
        var synchronizer = new TestSynchronizer { ShouldEnterSucceed = true };
        using var sp = CreateServiceProvider(synchronizer);
        var svc = new TestSynchronizedTimerService(sp, NullLogger.Instance) { Interval = TimeSpan.FromMilliseconds(20), FirstInterval = TimeSpan.FromMilliseconds(5) };

        await svc.StartAsync(CancellationToken.None);
        try
        {
            await WaitUntilAsync(() => svc.ExecuteCount > 0, TimeSpan.FromSeconds(5));
            svc.ExecuteCount.Should().BeGreaterThan(0);
            synchronizer.EnterCount.Should().BeGreaterThan(0);
        }
        finally
        {
            await svc.StopAsync(CancellationToken.None);
        }
    }

    [Test]
    public async Task SynchronizedExecuteAsync_NotInvoked_WhenEnterFails()
    {
        var synchronizer = new TestSynchronizer { ShouldEnterSucceed = false };
        using var sp = CreateServiceProvider(synchronizer);
        var svc = new TestSynchronizedTimerService(sp, NullLogger.Instance) { Interval = TimeSpan.FromMilliseconds(20), FirstInterval = TimeSpan.FromMilliseconds(5) };

        await svc.StartAsync(CancellationToken.None);
        try
        {
            await WaitUntilAsync(() => synchronizer.EnterCount > 0, TimeSpan.FromSeconds(5));
            await Task.Delay(100); // Give any (incorrect) execution a chance to occur.
            svc.ExecuteCount.Should().Be(0);
            synchronizer.ExitCount.Should().Be(0);
        }
        finally
        {
            await svc.StopAsync(CancellationToken.None);
        }
    }

    [Test]
    public async Task ExitAsync_IsCalled_EvenWhenSynchronizedExecuteThrows()
    {
        var synchronizer = new TestSynchronizer { ShouldEnterSucceed = true };
        using var sp = CreateServiceProvider(synchronizer);
        var svc = new TestSynchronizedTimerService(sp, NullLogger.Instance)
        {
            Interval = TimeSpan.FromMilliseconds(20),
            FirstInterval = TimeSpan.FromMilliseconds(5),
            ThrowOnExecute = true
        };

        await svc.StartAsync(CancellationToken.None);
        try
        {
            await WaitUntilAsync(() => synchronizer.ExitCount > 0, TimeSpan.FromSeconds(5));
            synchronizer.ExitCount.Should().BeGreaterThan(0);
            synchronizer.EnterCount.Should().Be(synchronizer.ExitCount);
        }
        finally
        {
            await svc.StopAsync(CancellationToken.None);
        }
    }

    [Test]
    public async Task SynchronizerName_DefaultsToNull()
    {
        var synchronizer = new TestSynchronizer { ShouldEnterSucceed = true };
        using var sp = CreateServiceProvider(synchronizer);
        var svc = new TestSynchronizedTimerService(sp, NullLogger.Instance) { Interval = TimeSpan.FromMilliseconds(20), FirstInterval = TimeSpan.FromMilliseconds(5) };

        await svc.StartAsync(CancellationToken.None);
        try
        {
            await WaitUntilAsync(() => synchronizer.EnterCount > 0, TimeSpan.FromSeconds(5));
            synchronizer.LastEnterName.Should().BeNull();
        }
        finally
        {
            await svc.StopAsync(CancellationToken.None);
        }
    }

    [Test]
    public async Task SynchronizerName_WhenSet_IsPassedToEnterAndExit()
    {
        var synchronizer = new TestSynchronizer { ShouldEnterSucceed = true };
        using var sp = CreateServiceProvider(synchronizer);
        var svc = new TestSynchronizedTimerService(sp, NullLogger.Instance)
        {
            Interval = TimeSpan.FromMilliseconds(20),
            FirstInterval = TimeSpan.FromMilliseconds(5),
            SynchronizerNameOverride = "custom-name"
        };

        await svc.StartAsync(CancellationToken.None);
        try
        {
            await WaitUntilAsync(() => synchronizer.ExitCount > 0, TimeSpan.FromSeconds(5));
            synchronizer.LastEnterName.Should().Be("custom-name");
            synchronizer.LastExitName.Should().Be("custom-name");
        }
        finally
        {
            await svc.StopAsync(CancellationToken.None);
        }
    }

    [Test]
    public async Task EnterAndExit_UseSelfTypeAsLockType()
    {
        var synchronizer = new TestSynchronizer { ShouldEnterSucceed = true };
        using var sp = CreateServiceProvider(synchronizer);
        var svc = new TestSynchronizedTimerService(sp, NullLogger.Instance) { Interval = TimeSpan.FromMilliseconds(20), FirstInterval = TimeSpan.FromMilliseconds(5) };

        await svc.StartAsync(CancellationToken.None);
        try
        {
            await WaitUntilAsync(() => synchronizer.ExitCount > 0, TimeSpan.FromSeconds(5));
            synchronizer.LastEnterType.Should().Be(typeof(TestSynchronizedTimerService));
            synchronizer.LastExitType.Should().Be(typeof(TestSynchronizedTimerService));
        }
        finally
        {
            await svc.StopAsync(CancellationToken.None);
        }
    }

    private class TestSynchronizer : ISynchronizer
    {
        private int _enterCount;
        private int _exitCount;

        public bool ShouldEnterSucceed { get; set; }

        public int EnterCount => _enterCount;

        public int ExitCount => _exitCount;

        public string? LastEnterName { get; private set; }

        public string? LastExitName { get; private set; }

        public Type? LastEnterType { get; private set; }

        public Type? LastExitType { get; private set; }

        public Task<bool> EnterAsync<T>(string? name = null, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _enterCount);
            LastEnterName = name;
            LastEnterType = typeof(T);
            return Task.FromResult(ShouldEnterSucceed);
        }

        public Task ExitAsync<T>(string? name = null, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _exitCount);
            LastExitName = name;
            LastExitType = typeof(T);
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private class TestSynchronizedTimerService(IServiceProvider serviceProvider, ILogger logger)
        : SynchronizedTimerHostedServiceBase<TestSynchronizer, TestSynchronizedTimerService>(serviceProvider, logger)
    {
        private int _executeCount;

        public int ExecuteCount => _executeCount;

        public bool ThrowOnExecute { get; set; }

        public string? SynchronizerNameOverride
        {
            get => SynchronizerName;
            set => SynchronizerName = value;
        }

        protected override Task<bool> SynchronizedExecuteAsync(ExecutionContext executionContext, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _executeCount);

            if (ThrowOnExecute)
                throw new InvalidOperationException("Test failure.");

            return Task.FromResult(false);
        }
    }
}
