#pragma warning disable IDE0130 // Namespace does not match folder structure; by design.
namespace Microsoft.AspNetCore.Builder;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Provides standard extensions.
/// </summary>
public static partial class CoreExAspNetCoreExtensions
{
    /// <summary>
    /// Adds a middleware action to <paramref name="configure"/> the <see cref="ExecutionContext"/> per request. 
    /// </summary>
    /// <param name="builder">The <see cref="IApplicationBuilder"/>.</param>
    /// <param name="configure">An optional function to update the <see cref="ExecutionContext"/>.</param>
    /// <returns>The <see cref="IApplicationBuilder"/> for fluent-style method-chaining.</returns>
    public static IApplicationBuilder UseExecutionContext(this IApplicationBuilder builder, Func<HttpContext, ExecutionContext, Task>? configure = null)
        => builder.ThrowIfNull().UseMiddleware<ExecutionContextMiddleware>(configure ?? ExecutionContextMiddleware.DefaultConfigure);

    /// <summary>
    /// Registers the standard ASP.NET Core exception handling middleware leveraging the <i>CoreEx</i> <see cref="ExceptionHandlingMiddleware.ConvertExceptionToProblemDetails(IApplicationBuilder)"/> to convert the <see cref="Exception"/> into a <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IApplicationBuilder"/>.</param>
    /// <returns>The <see cref="IApplicationBuilder"/> for fluent-style method-chaining.</returns>
    /// <remarks>This is primarily required to support specific <see cref="IExtendedException"/> handling.
    /// <para>Is effectively a convenience method for <see cref="ExceptionHandlerExtensions.UseExceptionHandler(IApplicationBuilder)"/>, i.e.:
    /// <code>    builder.UseExceptionHandler(CoreEx.AspNetCore.ExceptionHandlingMiddleware.ConvertExceptionToProblemDetails);</code></para></remarks>
    public static IApplicationBuilder UseCoreExExceptionHandler(this IApplicationBuilder builder)
        => builder.ThrowIfNull().UseExceptionHandler(ExceptionHandlingMiddleware.ConvertExceptionToProblemDetails);

    /// <summary>
    /// Adds the standard <i>CoreEx</i> OpenTelemetry instrumentation using the application name as the service name.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/>.</param>
    /// <param name="serviceVersion">The optional version of the service; defaults to <c>1.0.0</c>.</param>
    /// <returns>The resulting <see cref="OpenTelemetryBuilder"/> to support fluent-style method-chaining.</returns>
    /// <remarks>Includes <see cref="OpenTelemetry.Trace.CoreExExtensions.WithCoreExTelemetry(OpenTelemetryBuilder)"/> and <see cref="OpenTelemetry.Trace.CoreExAspNetCoreExtensions.WithAspNetCoreTelemetry(OpenTelemetryBuilder)"/>.</remarks>
    public static OpenTelemetryBuilder WithCoreExTelemetry(this IHostApplicationBuilder builder, string? serviceVersion = "1.0.0")
    {
        var telemetry = builder.ThrowIfNull().Services.AddOpenTelemetry().WithCoreExTelemetry().WithAspNetCoreTelemetry();
        telemetry.ConfigureResource(x => x.AddService(builder.Environment.ApplicationName, serviceVersion: serviceVersion));
        return telemetry;
    }

    /// <summary>
    /// Adds the <see cref="IdempotencyKeyMiddleware"/> to handle operations that required idempotency (see <see cref="CoreEx.AspNetCore.Mvc.IdempotencyKeyAttribute"/>).
    /// </summary>
    /// <param name="builder">The <see cref="IApplicationBuilder"/>.</param>
    /// <returns>The <see cref="IApplicationBuilder"/> for fluent-style method-chaining.</returns>
    public static IApplicationBuilder UseIdempotencyKey(this IApplicationBuilder builder)
        => builder.ThrowIfNull().UseMiddleware<IdempotencyKeyMiddleware>();

    /// <summary>   
    /// Maps health check endpoints enabling live, startup, and ready health checks with optional detailed responses as per the specified <paramref name="options"/>.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/>.</param>
    /// <param name="options">The optional <see cref="HealthCheckOptions"/>.</param>
    /// <param name="detailedGroupConfigure">An optional action to further configure the <i>detailed</i> health check endpoints, such as adding an authorization policy.</param>
    /// <returns>The <paramref name="endpoints"/> to support fluent-style method-chaining.</returns>
    /// <remarks>The <paramref name="options"/> default where not specified. Additionally, the <paramref name="options"/> are overridden using configuration settings where applicable.
    /// <para>The <i>live</i>, <i>startup</i>, and <i>ready</i> endpoints are intentionally left unauthorized by default, as they are conventionally probed anonymously by container orchestrators.
    /// The <i>detailed</i> endpoints (see <see cref="HealthCheckOptions.AreDetailedEndpointsEnabled"/>) are different: they emit the full <see cref="HealthReport"/>, which can include component names,
    /// connection details, and exception information, and should generally not be exposed publicly. Use <paramref name="detailedGroupConfigure"/> to secure them, e.g. <c>g =&gt; g.RequireAuthorization()</c>,
    /// once an authentication scheme and authorization services are registered.</para></remarks>
    public static IEndpointRouteBuilder MapHealthChecks(this IEndpointRouteBuilder endpoints, HealthCheckOptions? options = null, Action<IEndpointConventionBuilder>? detailedGroupConfigure = null)
    {
        endpoints.ThrowIfNull();

        // Bind options with configuration; also, default options where applicable.
        options ??= new HealthCheckOptions();
        endpoints.ServiceProvider.GetService<IConfiguration>()?.GetSection("CoreEx.AspNetCore.HealthChecks")?.Bind(options);

        // Map health checks for the specified path and tags.
        void MapHealthChecks(string path, string[] tags)
        {
            endpoints.MapHealthChecks(path, new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                Predicate = check => CheckRegistration(check, tags),
            });

            if (options.AreDetailedEndpointsEnabled)
            {
                var detailed = endpoints.MapHealthChecks($"{path.TrimEnd('/')}/{options.DetailedPathSuffix.TrimStart('/')}", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
                {
                    Predicate = check => CheckRegistration(check, tags),
                    ResponseWriter = options.OnWriteDetailedHealthCheckAsync
                });

                detailedGroupConfigure?.Invoke(detailed);
            }
        }

        // Check and update the registration.
        bool CheckRegistration(HealthCheckRegistration rego, string[] tags)
        { 
            if (rego.Tags is not null && rego.Tags.Any(tag => tags.Contains(tag)))
            {
                options.OnConfigureHealthCheckRegistration(rego);
                return true;
            }

            return false;
        }

        // Map the configured health check endpoints.
        if (options.IsLiveEndpointEnabled)
            MapHealthChecks(options.LivePath, options.LiveTags);

        if (options.IsStartupEndpointEnabled)
            MapHealthChecks(options.StartupPath, options.StartupTags);

        if (options.IsReadyEndpointEnabled)
            MapHealthChecks(options.ReadyPath, options.ReadyTags);

        return endpoints;
    }

    /// <summary>
    /// Maps endpoints for managing <see cref="HostedServiceBase"/> services, including status retrieval, pause, and resume operations.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/>.</param>
    /// <param name="routeName">The base route name for the hosted services endpoints.</param>
    /// <param name="groupConfigure">An optional action to configure the <see cref="RouteGroupBuilder"/>.</param>
    /// <returns>The <see cref="IEndpointRouteBuilder"/> to support fluent-style method-chaining.</returns>
    /// <remarks>The mapped endpoints are excluded from OpenAPI documentation by default, as they are typically intended for administrative use only. The <paramref name="groupConfigure"/> allows additional
    /// configuration, such as adding authorization policies, etc. which is highly recommended.</remarks>
    public static IEndpointRouteBuilder MapHostedServices(this IEndpointRouteBuilder endpoints, string routeName = "/hosted-services", Action<RouteGroupBuilder>? groupConfigure = null)
    {
        // Map the endpoint group and hide from OpenAPI description - these are generally for admin use only!
        var group = endpoints.ThrowIfNull().MapGroup(routeName.ThrowIfNullOrEmpty()).ExcludeFromDescription();

        // Provides the all "hosted services" status management.
        group.MapGet("/all/status", (HttpRequest request, CoreEx.AspNetCore.Http.WebApi webApi, [Microsoft.AspNetCore.Mvc.FromServices] HostedServiceManager manager, CancellationToken cancellationToken)
            => webApi.GetWithResultAsync(request, (_, CancellationToken) => manager.ThrowIfNull().GetAllStatusesAsync(cancellationToken), cancellationToken: cancellationToken));

        group.MapPost("/all/pause", (HttpRequest request, CoreEx.AspNetCore.Http.WebApi webApi, [Microsoft.AspNetCore.Mvc.FromServices] HostedServiceManager manager, CancellationToken cancellationToken)
            => webApi.PostWithResultAsync(request, (_, CancellationToken) => manager.ThrowIfNull().PauseAllAsync(cancellationToken), HttpStatusCode.Accepted, cancellationToken: cancellationToken));

        group.MapPost("/all/resume", (HttpRequest request, CoreEx.AspNetCore.Http.WebApi webApi, [Microsoft.AspNetCore.Mvc.FromServices] HostedServiceManager manager, CancellationToken cancellationToken)
            => webApi.PostWithResultAsync(request, (_, CancellationToken) => manager.ThrowIfNull().ResumeAllAsync(cancellationToken), HttpStatusCode.Accepted, cancellationToken: cancellationToken));

        // Provides the per "hosted service" status management.
        group.MapGet("/{serviceKey}/status", (HttpRequest request, CoreEx.AspNetCore.Http.WebApi webApi, [Microsoft.AspNetCore.Mvc.FromServices] HostedServiceManager manager, string serviceKey, CancellationToken cancellationToken)
            => webApi.GetWithResultAsync(request, (_, CancellationToken) => manager.ThrowIfNull().GetStatusAsync(serviceKey, cancellationToken), cancellationToken: cancellationToken));

        group.MapPost("/{serviceKey}/pause", (HttpRequest request, CoreEx.AspNetCore.Http.WebApi webApi, [Microsoft.AspNetCore.Mvc.FromServices] HostedServiceManager manager, string serviceKey, CancellationToken cancellationToken)
            => webApi.PostWithResultAsync(request, (_, CancellationToken) => manager.ThrowIfNull().PauseAsync(serviceKey, cancellationToken), HttpStatusCode.Accepted, cancellationToken: cancellationToken));

        group.MapPost("/{serviceKey}/resume", (HttpRequest request, CoreEx.AspNetCore.Http.WebApi webApi, [Microsoft.AspNetCore.Mvc.FromServices] HostedServiceManager manager, string serviceKey, CancellationToken cancellationToken)
            => webApi.PostWithResultAsync(request, (_, CancellationToken) => manager.ThrowIfNull().ResumeAsync(serviceKey, cancellationToken), HttpStatusCode.Accepted, cancellationToken: cancellationToken));

        // Enable further group configuration by the consuming developer, such as adding authorization policies, etc.
        groupConfigure?.Invoke(group);
        return endpoints;
    }
}
