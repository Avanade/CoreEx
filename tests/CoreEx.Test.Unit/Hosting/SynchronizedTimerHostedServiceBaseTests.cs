namespace CoreEx.Test.Unit.Hosting;

[TestFixture]
public class SynchronizedTimerHostedServiceBaseTests
{
    private static ServiceProvider CreateServiceProvider(TestSynchronizer synchronizer, IConfiguration? configuration = null)
    {
        var sc = new ServiceCollection();
        sc.AddSingleton(configuration ?? new ConfigurationBuilder().Build());
        sc.AddSingleton<IHostSettings>(new HostSettings { SolutionName = "Tests", DomainName = "orders", EnvironmentName = "Test" });
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
    public async Task SynchronizedExecuteAsync_IsInvoked_WhenLeaseIsAcquired()
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
    public async Task SynchronizedExecuteAsync_NotInvoked_OnLeaseContention()
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
    public async Task LeaseIsDisposed_EvenWhenSynchronizedExecuteThrows()
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
            synchronizer.LastInstanceId.Should().Be("default");
        }
        finally
        {
            await svc.StopAsync(CancellationToken.None);
        }
    }

    [Test]
    public async Task SynchronizerName_WhenSet_IsCapturedByLease()
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
            synchronizer.LastInstanceId.Should().Be("name:custom-name");
            synchronizer.LastExitInstanceId.Should().Be(synchronizer.LastInstanceId);
        }
        finally
        {
            await svc.StopAsync(CancellationToken.None);
        }
    }

    [Test]
    public async Task LeaseKey_UsesSelfType()
    {
        var synchronizer = new TestSynchronizer { ShouldEnterSucceed = true };
        using var sp = CreateServiceProvider(synchronizer);
        var svc = new TestSynchronizedTimerService(sp, NullLogger.Instance) { Interval = TimeSpan.FromMilliseconds(20), FirstInterval = TimeSpan.FromMilliseconds(5) };

        await svc.StartAsync(CancellationToken.None);
        try
        {
            await WaitUntilAsync(() => synchronizer.ExitCount > 0, TimeSpan.FromSeconds(5));
            synchronizer.LastOperationName.Should().Be(typeof(TestSynchronizedTimerService).FullName);
            synchronizer.LastExitInstanceId.Should().Be(synchronizer.LastInstanceId);
        }
        finally
        {
            await svc.StopAsync(CancellationToken.None);
        }
    }

    [Test]
    public async Task OwnershipLoss_CancelsProtectedExecution_AndDisposesLease()
    {
        using var loss = new CancellationTokenSource();
        var synchronizer = new TestSynchronizer { ShouldEnterSucceed = true, OwnershipToken = loss.Token };
        using var sp = CreateServiceProvider(synchronizer);
        var svc = new TestSynchronizedTimerService(sp, NullLogger.Instance)
        {
            Interval = TimeSpan.FromMilliseconds(20),
            FirstInterval = TimeSpan.FromMilliseconds(5),
            WaitForCancellation = true
        };
        await svc.StartAsync(CancellationToken.None);
        try
        {
            await WaitUntilAsync(() => svc.ExecuteCount > 0, TimeSpan.FromSeconds(5));
            loss.Cancel();
            await WaitUntilAsync(() => svc.CancellationObserved && synchronizer.ExitCount > 0, TimeSpan.FromSeconds(5));
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

        public CancellationToken OwnershipToken { get; set; }

        public int EnterCount => _enterCount;

        public int ExitCount => _exitCount;

        public string? LastOperationName { get; private set; }

        public string? LastInstanceId { get; private set; }

        public string? LastExitInstanceId { get; private set; }

        public Task<ISynchronizationLease?> TryAcquireAsync(string name, CompositeKey key, CancellationToken cancellationToken = default)
        {
            LastOperationName = name;
            LastInstanceId = key.ToCanonicalString();
            Interlocked.Increment(ref _enterCount);
            return Task.FromResult<ISynchronizationLease?>(ShouldEnterSucceed ? new TestLease(this) : null);
        }

        public ValueTask ExitAsync()
        {
            // Set the values before incrementing the count, as tests poll the count and then read the values.
            LastExitInstanceId = LastInstanceId;
            Interlocked.Increment(ref _exitCount);
            return ValueTask.CompletedTask;
        }

        private sealed class TestLease(TestSynchronizer owner) : ISynchronizationLease
        {
            public CancellationToken OwnershipLost => owner.OwnershipToken;

            public ValueTask DisposeAsync() => owner.ExitAsync();
        }
    }

    private class TestSynchronizedTimerService(IServiceProvider serviceProvider, ILogger logger)
        : SynchronizedTimerHostedServiceBase<TestSynchronizer, TestSynchronizedTimerService>(serviceProvider, logger)
    {
        private int _executeCount;
        private int _cancellationObserved;

        public int ExecuteCount => _executeCount;

        public bool ThrowOnExecute { get; set; }

        public bool WaitForCancellation { get; set; }

        public bool CancellationObserved => Volatile.Read(ref _cancellationObserved) != 0;

        public string? SynchronizerNameOverride
        {
            get => SynchronizerName;
            set => SynchronizerName = value;
        }

        protected override async Task<bool> SynchronizedExecuteAsync(ExecutionContext executionContext, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _executeCount);

            if (ThrowOnExecute)
                throw new InvalidOperationException("Test failure.");

            if (WaitForCancellation)
            {
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    Interlocked.Exchange(ref _cancellationObserved, 1);
                    throw;
                }
            }

            return false;
        }
    }
}
