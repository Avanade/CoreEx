namespace Contoso.E2E.Runner.Infrastructure;

/// <summary>
/// Configuration for load simulation settings.
/// </summary>
public class LoadSimulationConfig
{
    /// <summary>
    /// Gets or sets the number of recent events to display.
    /// </summary>
    public int RecentEventsDisplayCount { get; set; } = 5;

    /// <summary>
    /// Gets or sets the collection of simulator configuration settings, indexed by simulator name.
    /// </summary>
    public Dictionary<string, LoadSimulationSimulatorConfig> Simulations { get; set; } = [];
}