using CoreEx.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;

namespace CoreEx.Test.Unit.Hosting;

[TestFixture]
public class HostedServiceBaseTests
{
    private static ServiceProvider CreateServiceProvider(IConfiguration? configuration = null)
    {
        var sc = new ServiceCollection();
        sc.AddSingleton(configuration ?? new ConfigurationBuilder().Build());
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
    public async Task StartAsync_TransitionsToRunning()
    {
        using var sp = CreateServiceProvider();
        var svc = new TestHostedService(sp, NullLogger.Instance);

        await svc.StartAsync(CancellationToken.None);

        svc.Status.Should().Be(ServiceStatus.Running);
        svc.OnStartAsyncCalled.Should().BeTrue();
    }

    [Test]
    public async Task StartAsync_NoOpConfigured_SetsNoOpStatus_SkipsNormalStart()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { { HostedServiceBase.NoOpArgument, "true" } }).Build();
        using var sp = CreateServiceProvider(config);
        var svc = new TestHostedService(sp, NullLogger.Instance);

        await svc.StartAsync(CancellationToken.None);

        svc.Status.Should().Be(ServiceStatus.NoOp);
        svc.OnStartAsyncCalled.Should().BeFalse();
    }

    [Test]
    public async Task StopAsync_TransitionsThroughStoppingBeforeStopped()
    {
        using var sp = CreateServiceProvider();
        var svc = new TestHostedService(sp, NullLogger.Instance);

        await svc.StartAsync(CancellationToken.None);
        svc.Status.Should().Be(ServiceStatus.Running);

        // OnStopAsync captures the Status synchronously before awaiting the gate, so by the time StopAsync
        // suspends (on the incomplete gate), StatusDuringStop already reflects the mid-stop state.
        var stopTask = svc.StopAsync(CancellationToken.None);
        svc.StatusDuringStop.Should().Be(ServiceStatus.Stopping);

        svc.StopGate.SetResult();
        await stopTask;

        svc.Status.Should().Be(ServiceStatus.Stopped);
    }

    [Test]
    public async Task StopAsync_FromAlreadyStoppedStatus_StillCompletes()
    {
        using var sp = CreateServiceProvider();
        var svc = new TestHostedService(sp, NullLogger.Instance);

        await svc.StartAsync(CancellationToken.None);
        svc.StopGate.SetResult();
        await svc.StopAsync(CancellationToken.None);

        // A second stop, from an already-Stopped status, must still transition through Stopping and complete.
        svc.StopGate = new TaskCompletionSource();
        svc.StopGate.SetResult();
        await svc.StopAsync(CancellationToken.None);

        svc.StatusDuringStop.Should().Be(ServiceStatus.Stopping);
        svc.Status.Should().Be(ServiceStatus.Stopped);
    }

    [Test]
    public async Task PauseAsync_NotSupported_Throws()
    {
        using var sp = CreateServiceProvider();
        var svc = new TestHostedService(sp, NullLogger.Instance); // ArePauseAndResumeSupported defaults to false.
        await svc.StartAsync(CancellationToken.None);

        Func<Task> act = () => svc.PauseAsync(CancellationToken.None);
        await act.Should().ThrowAsync<NotSupportedException>();
    }

    [Test]
    public async Task PauseAsync_ThenResumeAsync_TransitionsCorrectly()
    {
        using var sp = CreateServiceProvider();
        var svc = new TestHostedService(sp, NullLogger.Instance) { SupportsPauseAndResume = true };
        await svc.StartAsync(CancellationToken.None);
        svc.Status.Should().Be(ServiceStatus.Running);

        await svc.PauseAsync(CancellationToken.None);
        svc.Status.Should().Be(ServiceStatus.Paused);

        await svc.ResumeAsync(CancellationToken.None);
        svc.Status.Should().Be(ServiceStatus.Running);
    }

    [Test]
    public async Task PauseAsync_WhenNotPausable_IsNoOp()
    {
        using var sp = CreateServiceProvider();
        var svc = new TestHostedService(sp, NullLogger.Instance) { SupportsPauseAndResume = true };
        await svc.StartAsync(CancellationToken.None);
        svc.StopGate.SetResult();
        await svc.StopAsync(CancellationToken.None); // Status is now Stopped, which cannot be paused.

        await svc.PauseAsync(CancellationToken.None);
        svc.Status.Should().Be(ServiceStatus.Stopped);
    }

    [Test]
    public async Task Pause_FireAndForget_EventuallyPauses()
    {
        using var sp = CreateServiceProvider();
        var svc = new TestHostedService(sp, NullLogger.Instance) { SupportsPauseAndResume = true };
        await svc.StartAsync(CancellationToken.None);

        svc.Pause();

        await WaitUntilAsync(() => svc.Status == ServiceStatus.Paused, TimeSpan.FromSeconds(2));
        svc.Status.Should().Be(ServiceStatus.Paused);
    }

    [Test]
    public async Task Resume_FireAndForget_EventuallyResumes()
    {
        using var sp = CreateServiceProvider();
        var svc = new TestHostedService(sp, NullLogger.Instance) { SupportsPauseAndResume = true };
        await svc.StartAsync(CancellationToken.None);
        await svc.PauseAsync(CancellationToken.None);

        svc.Resume();

        await WaitUntilAsync(() => svc.Status == ServiceStatus.Running, TimeSpan.FromSeconds(2));
        svc.Status.Should().Be(ServiceStatus.Running);
    }

    [Test]
    public async Task ServiceName_CannotBeChanged_AfterInitializing()
    {
        using var sp = CreateServiceProvider();
        var svc = new TestHostedService(sp, NullLogger.Instance);
        await svc.StartAsync(CancellationToken.None);

        Action act = () => svc.ServiceName = "NewName";
        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public async Task HealthCheck_ReportsStatusOnEachChange()
    {
        using var sp = CreateServiceProvider();
        var svc = new TestHostedService(sp, NullLogger.Instance);
        var hc = new HostedServiceHealthCheck();
        svc.HealthCheck = hc; // Only settable while Initializing.

        await svc.StartAsync(CancellationToken.None);

        svc.LastReportedStatus.Should().Be(ServiceStatus.Running);
        hc.Result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Test]
    public async Task Dispose_SetsStatusToStopped_AndIsIdempotent()
    {
        using var sp = CreateServiceProvider();
        var svc = new TestHostedService(sp, NullLogger.Instance);
        await svc.StartAsync(CancellationToken.None);

        svc.Dispose();
        svc.Status.Should().Be(ServiceStatus.Stopped);

        Action act = () => svc.Dispose();
        act.Should().NotThrow();
    }

    private class TestHostedService(IServiceProvider serviceProvider, ILogger logger) : HostedServiceBase(serviceProvider, logger)
    {
        public TaskCompletionSource StopGate { get; set; } = new();

        public ServiceStatus StatusDuringStop { get; private set; }

        public bool OnStartAsyncCalled { get; private set; }

        public ServiceStatus? LastReportedStatus { get; private set; }

        public bool SupportsPauseAndResume
        {
            get => ArePauseAndResumeSupported;
            set => ArePauseAndResumeSupported = value;
        }

        protected override Task<ServiceStatus> OnStartAsync(CancellationToken cancellationToken)
        {
            OnStartAsyncCalled = true;
            return Task.FromResult(ServiceStatus.Running);
        }

        protected override async Task OnStopAsync(CancellationToken cancellationToken)
        {
            StatusDuringStop = Status;
            await StopGate.Task;
        }

        protected override Task OnPauseAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        protected override Task OnResumeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        protected override HealthCheckResult OnReportHealthStatus(Dictionary<string, object> data)
        {
            LastReportedStatus = Enum.Parse<ServiceStatus>(data["status"].ToString()!);
            return HealthCheckResult.Healthy();
        }
    }
}
