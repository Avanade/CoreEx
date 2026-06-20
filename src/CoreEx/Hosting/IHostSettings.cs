namespace CoreEx.Hosting;

/// <summary>
/// Enables standardized runtime/host settings.
/// </summary>
public interface IHostSettings
{
    /// <summary>
    /// Gets the solution name.
    /// </summary>
    string SolutionName { get; }

    /// <summary>
    /// Gets the domain name. 
    /// </summary>
    string DomainName { get; }

    /// <summary>
    /// Gets the environment name.
    /// </summary>
    /// <remarks>This is automatically set by the following environment variables: <c>COREEX_ENVIRONMENT</c> (primary) or <c>ASPNETCORE_ENVIRONMENT</c> (secondary).</remarks>
    string EnvironmentName { get; }

    /// <summary>
    /// Gets the source <see cref="Uri"/>.
    /// </summary>
    /// <remarks>This typically represents the base application/service URL used for the likes of event source.</remarks>
    Uri? Source { get; }
}