namespace CoreEx.AspNetCore.HealthChecks;

/// <summary>
/// Provides configuration options for health checks.
/// </summary>
/// <remarks>Additionally, override the <see cref="OnWriteDetailedHealthCheckAsync(HttpContext, HealthReport)"/> to change the default detailed <see cref="HealthReport"/>.</remarks>
public class HealthCheckOptions
{
    /// <summary>
    /// Indicates whether the <i>live</i> health check endpoint is enabled.
    /// </summary>
    public bool IsLiveEndpointEnabled { get; set; } = true;

    /// <summary>
    /// Indicates whether the <i>startup</i> health check endpoint is enabled.
    /// </summary>
    public bool IsStartupEndpointEnabled { get; set; } = true;

    /// <summary>
    /// Indicates whether the <i>ready</i> health check endpoint is enabled.
    /// </summary>
    public bool IsReadyEndpointEnabled { get; set; } = true;

    /// <summary>
    /// Indicates whether the <i>detailed</i> health check endpoints are enabled.
    /// </summary>
    public bool AreDetailedEndpointsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the <i>live</i> health check path.
    /// </summary>
    public string LivePath { get; set; } = "/health/live";

    /// <summary>
    /// Gets or sets the <i>startup</i> health check path.
    /// </summary>
    public string StartupPath { get; set; } = "/health/startup";

    /// <summary>
    /// Gets or sets the <i>ready</i> health check path.
    /// </summary>
    public string ReadyPath { get; set; } = "/health/ready";

    /// <summary>
    /// Gets or sets the <i>detailed</i> path suffix.
    /// </summary>
    public string DetailedPathSuffix { get; set; } = "detailed";

    /// <summary>
    /// Gets or sets the <i>live</i> health check tags.
    /// </summary>
    public string[] LiveTags { get; set; } = [nameof(HealthCheckTags.Live)];

    /// <summary>
    /// Gets or sets the <i>startup</i> health check tags.
    /// </summary>
    public string[] StartupTags { get; set; } = [nameof(HealthCheckTags.Startup)];

    /// <summary>
    /// Gets or sets the <i>ready</i> health check tags.
    /// </summary>
    public string[] ReadyTags { get; set; } = [nameof(HealthCheckTags.Ready)];

    /// <summary>
    /// Gets or sets the <see cref="JsonSerializerOptions"/> used for <see cref="AreDetailedEndpointsEnabled">detailed</see> health check responses.
    /// </summary>
    /// <remarks>This defaults to a clone of the <see cref="Json.JsonDefaults.SerializerOptions"/> extending the <see cref="JsonSerializerOptions.Converters"/> to include the specialized
    /// <see cref="Json.JsonExceptionConverterFactory"/> for when a <see cref="HealthReportEntry.Exception"/> is being reported.</remarks>
    public JsonSerializerOptions? JsonSerializerOptions { get; set; }

    /// <summary>
    /// Indicates the default <i>detailed</i> health check JSON response should be pretty printed.
    /// </summary>
    /// <remarks>This sets the underlying <see cref="JsonSerializerOptions.WriteIndented"/> value accordingly.</remarks>
    public bool? PrettyPrint { get; set; } = true;

    /// <summary>
    /// Gets (creates) the <see cref="JsonSerializerOptions"/>.
    /// </summary>
    /// <returns></returns>
    private JsonSerializerOptions GetJsonSerializerOptions()
    {
        if (JsonSerializerOptions is not null)
            return JsonSerializerOptions;

        var jso = new JsonSerializerOptions(JsonDefaults.SerializerOptions);
        jso.Converters.Add(new JsonExceptionConverterFactory());

        if (PrettyPrint.HasValue)
            jso.WriteIndented = PrettyPrint.Value;

        return jso;
    }

    /// <summary>
    /// Writes the <i>detailed</i> health check response from the <paramref name="report"/>.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <param name="report">The <see cref="HealthReport"/>.</param>
    /// <remarks>This will only be invoked where accessing a valid detailed endpoint where <see cref="AreDetailedEndpointsEnabled"/> is <see langword="true"/>.
    /// <para>The default implementation simply JSON serializes the <paramref name="report"/> as the response using the <see cref="JsonSerializerOptions"/>.</para></remarks>
    public virtual async Task OnWriteDetailedHealthCheckAsync(HttpContext context, HealthReport report)
        => await context.Response.WriteAsJsonAsync(report, GetJsonSerializerOptions()).ConfigureAwait(false);

    /// <summary>
    /// Provides an opportunity to further configure the health check <paramref name="registration"/>.
    /// </summary>
    /// <param name="registration">The <see cref="HealthCheckRegistration"/>.</param>
    public virtual void OnConfigureHealthCheckRegistration(HealthCheckRegistration registration) { }
}