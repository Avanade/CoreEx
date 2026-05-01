namespace CoreEx.Hosting;

/// <summary>
/// Provides standardized runtime/host settings.
/// </summary>
public class HostSettings : IHostSettings
{
    private Uri? _source;

    /// <summary>
    /// Creates a new <see cref="HostSettings"/> using the <paramref name="configuration"/> to realize the <see cref="SolutionName"/> and <see cref="DomainName"/> properties where not specifically provided.
    /// </summary>
    /// <param name="configuration">The <see cref="IConfiguration"/>.</param>
    /// <param name="environmentName">The environment name; for example '<c>Development</c>'.</param>
    /// <param name="solutionName">The area name; for example '<c>contoso</c>'.</param>
    /// <param name="domainName">The domain name; for example '<c>products</c>'.</param>
    /// <param name="source">The source <see cref="Uri"/>; for example '<c>urn:contoso:products</c>'.</param>
    /// <returns>The <see cref="HostSettings"/>.</returns>
    public static HostSettings Create(IConfiguration configuration, string environmentName, string? solutionName = null, string? domainName = null, Uri? source = null)
    {
        configuration.ThrowIfNull();

        var sn = solutionName ?? Internal.GetConfigurationValue<string>("CoreEx:Host:SolutionName", null, configuration) ?? throw new ArgumentException($"{nameof(SolutionName)} must either be specified or configured 'CoreEx:Host:SolutionName'");
        var dn = domainName ?? Internal.GetConfigurationValue<string>("CoreEx:Host:DomainName", null, configuration) ?? throw new ArgumentException($"{nameof(DomainName)} must either be specified or configured 'CoreEx:Host:DomainName'");
        var su = source ?? Internal.GetConfigurationValue<Uri?>("CoreEx:Host:Source", null, configuration);

        return new HostSettings
        {
            SolutionName = sn,
            DomainName = dn,
            EnvironmentName = environmentName,
            Source = su
        };
    }

    /// <inheritdoc/>
    public required string SolutionName { get; init => field = value.ThrowIfNullOrEmpty(); }

    /// <inheritdoc/>
    public required string DomainName { get; init => field = value.ThrowIfNullOrEmpty(); }

    /// <inheritdoc/>
    public required string EnvironmentName { get; init => field = value.ThrowIfNullOrEmpty(); }

    /// <inheritdoc/>
    public Uri? Source
    {
        get => _source ??= new Uri($"urn:{SolutionName.Replace('.', ':').ToLowerInvariant()}:{DomainName.ToLowerInvariant()}");
        init => _source = value;
    }
}