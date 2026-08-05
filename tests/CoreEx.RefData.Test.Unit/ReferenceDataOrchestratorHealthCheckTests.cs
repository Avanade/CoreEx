using CoreEx.RefData.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CoreEx.RefData.Test.Unit;

public partial class ReferenceDataOrchestratorTests
{
    [Test]
    public async Task CheckHealthAsync_ReturnsHealthy_WithRegisteredTypes()
    {
        var orch = CreateOrchestrator();
        var healthCheck = new ReferenceDataOrchestratorHealthCheck(orch);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data.Should().ContainKey("types");
        ((string[])result.Data["types"]).Should().BeEquivalentTo(nameof(DummyRefData), nameof(DummyRefData2));
    }

    [Test]
    public void Constructor_NullOrchestrator_Throws()
    {
        Action act = () => new ReferenceDataOrchestratorHealthCheck(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
