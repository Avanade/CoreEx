#pragma warning disable IDE0130 // Namespace does not match folder structure; by design.
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Provides the <see href="https://github.com/RicoSuter/NSwag"/> extensions.
/// </summary>
public static class CoreExNSwagExtensions
{
    /// <summary>
    /// Adds the <i>CoreEx</i>-specific <i>OpenAPI</i> configuration.
    /// </summary>
    /// <param name="settings">The <see cref="OpenApiDocumentGeneratorSettings"/>.</param>
    /// <returns>The <see cref="OpenApiDocumentGeneratorSettings"/> for fluent-style method-chaining.</returns>
    /// <remarks>This is a shortcut for calling both <see cref="AddOpenApiDocumentExtensions(OpenApiDocumentGeneratorSettings, Action{OpenApiOptions}?)"/> and <see cref="ConfigureSchemaSettings(OpenApiDocumentGeneratorSettings, JsonSerializerOptions?)"/>.</remarks>
    public static OpenApiDocumentGeneratorSettings AddCoreExConfiguration(this OpenApiDocumentGeneratorSettings settings) => settings.AddOpenApiDocumentExtensions().ConfigureSchemaSettings();

    /// <summary>
    /// Adds the <i>CoreEx</i>-specific <i>OpenAPI</i> generated specification configuration extensions.
    /// </summary>
    /// <param name="settings">The <see cref="OpenApiDocumentGeneratorSettings"/>.</param>
    /// <param name="configure">An optional action to configure the <see cref="OpenApiOptions"/>.</param>
    /// <returns>The <see cref="OpenApiDocumentGeneratorSettings"/> for fluent-style method-chaining.</returns>
    public static OpenApiDocumentGeneratorSettings AddOpenApiDocumentExtensions(this OpenApiDocumentGeneratorSettings settings, Action<OpenApiOptions>? configure = null)
    {
        settings.ThrowIfNull();

        var options = new OpenApiOptions();
        configure?.Invoke(options);

        settings.OperationProcessors.Add(new NSwagOpenApiOperationProcessor(options));
        return settings;
    }

    /// <summary>
    /// Configures the <see cref="OpenApiDocumentGeneratorSettings.SchemaSettings"/> to use the <see cref="JsonDefaults.SerializerOptions"/> (unless specifically overridden).
    /// </summary>
    /// <param name="settings">The <see cref="OpenApiDocumentGeneratorSettings"/>.</param>
    /// <param name="jsonSerializerOptions">The optional <see cref="JsonSerializerOptions"/> override.</param>
    /// <returns>The <see cref="OpenApiDocumentGeneratorSettings"/> for fluent-style method-chaining.</returns>
    public static OpenApiDocumentGeneratorSettings ConfigureSchemaSettings(this OpenApiDocumentGeneratorSettings settings, JsonSerializerOptions? jsonSerializerOptions = null)
    {
        settings.ThrowIfNull();

        settings.SchemaSettings = new SystemTextJsonSchemaGeneratorSettings()
        {
            SchemaType = NJsonSchema.SchemaType.OpenApi3,
            SerializerOptions = jsonSerializerOptions ?? JsonDefaults.SerializerOptions
        };

        return settings;
    }
}