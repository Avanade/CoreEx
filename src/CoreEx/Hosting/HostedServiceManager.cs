namespace CoreEx.Hosting;

/// <summary>
/// Provides management capabilities for <see cref="HostedServiceBase"/> instances.
/// </summary>
/// <remarks>The <see cref="HostedServiceBase"/> instances must also be registered as a <see cref="IHostedService"/>.</remarks>
public sealed class HostedServiceManager(IServiceProvider serviceProvider)
{
    private readonly IServiceProvider _serviceProvider = serviceProvider.ThrowIfNull();

    /// <summary>
    /// Gets or sets the pre-check function that is invoked prior to any operation.
    /// </summary>
    /// <remarks>This function can be used to perform any necessary validation, such as authorization, before operations are executed on a hosted service. The <see cref="string"/> parameter is the selected <see cref="HostedServiceBase.ServiceName"/>
    /// where specified; otherwise, <see cref="string.Empty"/> represents <i>All</i>.</remarks>
    public Func<string, CancellationToken, Task<Result>> PreCheckAsync { get; set => field = value.ThrowIfNull(); } = (_, _) => Result.SuccessTask;

    /// <summary>
    /// Gets the status of all registered hosted services.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A dictionary of hosted service and corresponding service pairs.</returns>
    public Task<Result<Dictionary<string, ServiceStatus>>> GetAllStatusesAsync(CancellationToken cancellationToken = default)
        => PreCheckAsync(string.Empty, cancellationToken)
            .ThenAs(() =>
             {
                 var dict = new Dictionary<string, ServiceStatus>();
                 foreach (var service in GetAllHostedServices())
                 {
                     if (!dict.TryAdd(service.ServiceName, service.Status))
                         return Result.ValidationError($"The hosted service with key '{service.ServiceName}' is ambiguous; more than one exists with name.");
                 }

                 return Result<Dictionary<string, ServiceStatus>>.Ok(dict);
             });

    /// <summary>
    /// Pauses all registered hosted services.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <remarks>This is a fire-and-forget operation and each pause will occur in the background; therefore, it may immediately appear as if the status did not change.</remarks>
    public Task<Result> PauseAllAsync(CancellationToken cancellationToken = default)
        => PreCheckAsync(string.Empty, cancellationToken)
            .Then(() =>
            {
                foreach (var service in GetAllHostedServices())
                    service.Pause();
            });

    /// <summary>
    /// Resumes all registered hosted services.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <remarks>This is a fire-and-forget operation and each resume will occur in the background; therefore, it may immediately appear as if the status did not change.</remarks>
    public Task<Result> ResumeAllAsync(CancellationToken cancellationToken = default)
        => PreCheckAsync(string.Empty, cancellationToken)
            .Then(() =>
            {
                foreach (var service in GetAllHostedServices())
                    service.Resume();
            });

    /// <summary>
    /// Gets all the registered hosted services, optionally filtered by the specified service name.
    /// </summary>
    private IEnumerable<HostedServiceBase> GetAllHostedServices(string? serviceName = null)
        => _serviceProvider.GetServices<IHostedService>().OfType<HostedServiceBase>().Where(s => serviceName is null || s.ServiceName == serviceName);

    /// <summary>
    /// Gets the status of the specified hosted service.
    /// </summary>
    /// <param name="serviceKey">The unique hosted service key.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="ServiceStatus"/>.</returns>
    public Task<Result<ServiceStatus>> GetStatusAsync(string serviceKey, CancellationToken cancellationToken = default)
        => GetHostedServiceAsync(serviceKey, cancellationToken)
            .ThenAs(hs => hs.Status);

    /// <summary>
    /// Initiates a pause for the specified hosted service.
    /// </summary>
    /// <param name="serviceKey">The unique hosted service key.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <remarks>This is a fire-and-forget operation and will occur in the background; therefore, it may immediately appear as if the status did not change.</remarks>
    public Task<Result> PauseAsync(string serviceKey, CancellationToken cancellationToken = default)
        => GetHostedServiceAsync(serviceKey, cancellationToken)
            .ThenAs(hs => hs.Pause());

    /// <summary>
    /// Initiates a resume for the specified hosted service.
    /// </summary>
    /// <param name="serviceKey">The unique hosted service key.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <remarks>This is a fire-and-forget operation and will occur in the background; therefore, it may immediately appear as if the status did not change.</remarks>
    public Task<Result> ResumeAsync(string serviceKey, CancellationToken cancellationToken = default)
        => GetHostedServiceAsync(serviceKey, cancellationToken)
            .ThenAs(hs => hs.Resume());

    /// <summary>
    /// Gets the <see cref="HostedServiceBase"/> for the specified <paramref name="serviceKey"/>.
    /// </summary>
    /// <param name="serviceKey">The unique hosted service key.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="HostedServiceBase"/>.</returns>
    private Task<Result<HostedServiceBase>> GetHostedServiceAsync(string serviceKey, CancellationToken cancellationToken)
        => PreCheckAsync(serviceKey.ThrowIfNullOrEmpty(), cancellationToken)
            .ThenAs(() =>
            {
                var hsc = GetAllHostedServices(serviceKey).ToArray();
                return hsc.Length switch
                {
                    0 => Result.NotFoundError($"The hosted service with key '{serviceKey}' is not registered."),
                    1 => Result.Ok(hsc[0]),
                    _ => Result.ValidationError($"The hosted service with key '{serviceKey}' is ambiguous; more than one exists with name.")
                };
            });
}