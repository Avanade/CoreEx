namespace CoreEx.HealthChecks;

/// <summary>
/// Provides the standard health check tags.
/// </summary>
public enum HealthCheckTags
{
    /// <summary>
    /// Liveness probe.
    /// </summary>
    Live,

    /// <summary>
    /// Readiness probe.
    /// </summary>
    Ready,

    /// <summary>
    /// Startup probe.
    /// </summary>
    Startup
}