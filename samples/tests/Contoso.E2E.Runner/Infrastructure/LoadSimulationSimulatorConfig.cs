namespace Contoso.E2E.Runner.Infrastructure;

/// <summary>
/// Provides configuration options for the load simulation runner, including parallelism and delay settings between scenario iterations.
/// </summary>
public class LoadSimulationSimulatorConfig
{
    /// <summary>
    /// Gets or sets the number of parallel workers for the simulation.
    /// </summary>
    public int Parallelism { get; set; } = 1;

    /// <summary>
    /// Gets or sets the minimum delay in milliseconds between scenario iterations.
    /// </summary>
    public int MinDelayMilliseconds { get; set; } = 500;

    /// <summary>
    /// Gets or sets the maximum delay in milliseconds between scenario iterations.
    /// </summary>
    public int MaxDelayMilliseconds { get; set; } = 3000;
}