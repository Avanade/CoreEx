using CoreEx.Hosting;
using CoreEx.Results;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreEx.Test.Unit.Hosting;

[TestFixture]
public class HostedServiceManagerTests
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

    private static ServiceProvider CreateServiceProvider(params HostedServiceBase[] services)
    {
        var sc = new ServiceCollection();
        sc.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        foreach (var s in services)
            sc.AddSingleton<IHostedService>(s);

        return sc.BuildServiceProvider();
    }

    [Test]
    public async Task GetAllStatusesAsync_ReturnsStatusPerService()
    {
        using var sp = CreateServiceProvider();
        var alpha = new AlphaService(sp, NullLogger.Instance);
        var beta = new BetaService(sp, NullLogger.Instance);
        await alpha.StartAsync(CancellationToken.None);

        var manager = new HostedServiceManager(CreateServiceProvider(alpha, beta));
        var result = await manager.GetAllStatusesAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainKey("AlphaService").WhoseValue.Should().Be(ServiceStatus.Running);
        result.Value.Should().ContainKey("BetaService").WhoseValue.Should().Be(ServiceStatus.Initializing);
    }

    [Test]
    public async Task GetAllStatusesAsync_AmbiguousServiceName_ReturnsValidationError()
    {
        using var innerSp = CreateServiceProvider();
        var alpha1 = new AlphaService(innerSp, NullLogger.Instance);
        var alpha2 = new AlphaService(innerSp, NullLogger.Instance);

        var manager = new HostedServiceManager(CreateServiceProvider(alpha1, alpha2));
        var result = await manager.GetAllStatusesAsync();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationException>();
        result.Error!.Message.Should().Contain("ambiguous");
    }

    [Test]
    public async Task GetAllStatusesAsync_PreCheckFails_ReturnsFailureAndSkipsServices()
    {
        using var innerSp = CreateServiceProvider();
        var alpha = new AlphaService(innerSp, NullLogger.Instance);

        var manager = new HostedServiceManager(CreateServiceProvider(alpha))
        {
            PreCheckAsync = (_, _) => Task.FromResult(Result.ValidationError("blocked"))
        };

        var result = await manager.GetAllStatusesAsync();

        result.IsFailure.Should().BeTrue();
        result.Error!.Message.Should().Be("blocked");
    }

    [Test]
    public void PreCheckAsync_SetNull_Throws()
    {
        var manager = new HostedServiceManager(CreateServiceProvider());
        Action act = () => manager.PreCheckAsync = null!;
        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public async Task PreCheckAsync_InvokedWithEmptyKey_ForAllOperations()
    {
        string? captured = "not-called";
        var manager = new HostedServiceManager(CreateServiceProvider())
        {
            PreCheckAsync = (key, _) =>
            {
                captured = key;
                return Result.SuccessTask;
            }
        };

        await manager.GetAllStatusesAsync();

        captured.Should().Be(string.Empty);
    }

    [Test]
    public async Task PreCheckAsync_InvokedWithServiceKey_ForSingleServiceOperations()
    {
        using var innerSp = CreateServiceProvider();
        var alpha = new AlphaService(innerSp, NullLogger.Instance);
        string? captured = null;

        var manager = new HostedServiceManager(CreateServiceProvider(alpha))
        {
            PreCheckAsync = (key, _) =>
            {
                captured = key;
                return Result.SuccessTask;
            }
        };

        await manager.GetStatusAsync("AlphaService");

        captured.Should().Be("AlphaService");
    }

    [Test]
    public async Task PauseAllAsync_PausesAllSupportedServices()
    {
        using var innerSp = CreateServiceProvider();
        var alpha = new AlphaService(innerSp, NullLogger.Instance) { SupportsPauseAndResume = true };
        await alpha.StartAsync(CancellationToken.None);

        var manager = new HostedServiceManager(CreateServiceProvider(alpha));
        var result = await manager.PauseAllAsync();

        result.IsSuccess.Should().BeTrue();
        await WaitUntilAsync(() => alpha.Status == ServiceStatus.Paused, TimeSpan.FromSeconds(2));
    }

    [Test]
    public async Task ResumeAllAsync_ResumesAllSupportedServices()
    {
        using var innerSp = CreateServiceProvider();
        var alpha = new AlphaService(innerSp, NullLogger.Instance) { SupportsPauseAndResume = true };
        await alpha.StartAsync(CancellationToken.None);
        await alpha.PauseAsync(CancellationToken.None);

        var manager = new HostedServiceManager(CreateServiceProvider(alpha));
        var result = await manager.ResumeAllAsync();

        result.IsSuccess.Should().BeTrue();
        await WaitUntilAsync(() => alpha.Status == ServiceStatus.Running, TimeSpan.FromSeconds(2));
    }

    [Test]
    public async Task GetStatusAsync_UnknownKey_ReturnsNotFoundError()
    {
        var manager = new HostedServiceManager(CreateServiceProvider());
        var result = await manager.GetStatusAsync("Missing");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundException>();
    }

    [Test]
    public async Task GetStatusAsync_KnownKey_ReturnsStatus()
    {
        using var innerSp = CreateServiceProvider();
        var alpha = new AlphaService(innerSp, NullLogger.Instance);
        await alpha.StartAsync(CancellationToken.None);

        var manager = new HostedServiceManager(CreateServiceProvider(alpha));
        var result = await manager.GetStatusAsync("AlphaService");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(ServiceStatus.Running);
    }

    [Test]
    public async Task GetStatusAsync_AmbiguousKey_ReturnsValidationError()
    {
        using var innerSp = CreateServiceProvider();
        var alpha1 = new AlphaService(innerSp, NullLogger.Instance);
        var alpha2 = new AlphaService(innerSp, NullLogger.Instance);

        var manager = new HostedServiceManager(CreateServiceProvider(alpha1, alpha2));
        var result = await manager.GetStatusAsync("AlphaService");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationException>();
    }

    [Test]
    public async Task PauseAsync_KnownKey_EventuallyPauses()
    {
        using var innerSp = CreateServiceProvider();
        var alpha = new AlphaService(innerSp, NullLogger.Instance) { SupportsPauseAndResume = true };
        await alpha.StartAsync(CancellationToken.None);

        var manager = new HostedServiceManager(CreateServiceProvider(alpha));
        var result = await manager.PauseAsync("AlphaService");

        result.IsSuccess.Should().BeTrue();
        await WaitUntilAsync(() => alpha.Status == ServiceStatus.Paused, TimeSpan.FromSeconds(2));
    }

    [Test]
    public async Task ResumeAsync_KnownKey_EventuallyResumes()
    {
        using var innerSp = CreateServiceProvider();
        var alpha = new AlphaService(innerSp, NullLogger.Instance) { SupportsPauseAndResume = true };
        await alpha.StartAsync(CancellationToken.None);
        await alpha.PauseAsync(CancellationToken.None);

        var manager = new HostedServiceManager(CreateServiceProvider(alpha));
        var result = await manager.ResumeAsync("AlphaService");

        result.IsSuccess.Should().BeTrue();
        await WaitUntilAsync(() => alpha.Status == ServiceStatus.Running, TimeSpan.FromSeconds(2));
    }

    [Test]
    public async Task PauseAsync_UnknownKey_ReturnsNotFoundError()
    {
        var manager = new HostedServiceManager(CreateServiceProvider());
        var result = await manager.PauseAsync("Missing");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundException>();
    }

    private class AlphaService(IServiceProvider serviceProvider, ILogger logger) : HostedServiceBase(serviceProvider, logger)
    {
        public bool SupportsPauseAndResume
        {
            get => ArePauseAndResumeSupported;
            set => ArePauseAndResumeSupported = value;
        }

        protected override Task<ServiceStatus> OnStartAsync(CancellationToken cancellationToken) => Task.FromResult(ServiceStatus.Running);
        protected override Task OnStopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        protected override Task OnPauseAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        protected override Task OnResumeAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        protected override HealthCheckResult OnReportHealthStatus(Dictionary<string, object> data) => HealthCheckResult.Healthy();
    }

    private class BetaService(IServiceProvider serviceProvider, ILogger logger) : HostedServiceBase(serviceProvider, logger)
    {
        protected override Task<ServiceStatus> OnStartAsync(CancellationToken cancellationToken) => Task.FromResult(ServiceStatus.Running);
        protected override Task OnStopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        protected override Task OnPauseAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        protected override Task OnResumeAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        protected override HealthCheckResult OnReportHealthStatus(Dictionary<string, object> data) => HealthCheckResult.Healthy();
    }
}
