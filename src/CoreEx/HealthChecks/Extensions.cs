namespace CoreEx.HealthChecks;

/// <summary>
/// Provides extension methods for <see cref="HealthCheckTags"/>.
/// </summary>
public static class Extensions
{
    extension(HealthCheckTags healthCheckTags)
    {
        /// <summary>
        /// Gets the <see cref="HealthCheckTags.Startup"/> and <see cref="HealthCheckTags.Ready"/> tags.
        /// </summary>
        public static string[] StartUpAndReadyOnly => [nameof(HealthCheckTags.Startup), nameof(HealthCheckTags.Ready)];
    }
}