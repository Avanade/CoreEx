using CoreEx.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreEx.Test.Unit.Hosting;

[TestFixture]
public class HostedServiceBaseTests
{
    private static ServiceProvider CreateServiceProvider()
    {
        var sc = new ServiceCollection();
        sc.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        return sc.BuildServiceProvider();
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

    private class TestHostedService(IServiceProvider serviceProvider, ILogger logger) : HostedServiceBase(serviceProvider, logger)
    {
        public TaskCompletionSource StopGate { get; set; } = new();

        public ServiceStatus StatusDuringStop { get; private set; }

        protected override Task<ServiceStatus> OnStartAsync(CancellationToken cancellationToken) => Task.FromResult(ServiceStatus.Running);

        protected override async Task OnStopAsync(CancellationToken cancellationToken)
        {
            StatusDuringStop = Status;
            await StopGate.Task;
        }

        protected override HealthCheckResult OnReportHealthStatus(Dictionary<string, object> data) => HealthCheckResult.Healthy();
    }
}
