using CoreEx.Json;
using Microsoft.Extensions.DependencyInjection;
using NJsonSchema.Generation;
using NSwag.Generation;
using System.Text.Json;

namespace CoreEx.AspNetCore.Test.Unit;

public class NSwagExtensionsTests
{
    [Test]
    public void AddCoreExConfiguration_JsonSerializerOptions_FlowsToSchemaSettings()
    {
        // Regression: AddCoreExConfiguration() is documented as the single call required, but previously built its OpenApiOptions internally with no way to
        // supply a custom JsonSerializerOptions to it - ConfigureSchemaSettings() always fell back to JsonDefaults.SerializerOptions regardless. The configure
        // callback's OpenApiOptions.JsonSerializerOptions must now flow through to the resulting SchemaSettings.
        var customOptions = new JsonSerializerOptions();

        var settings = new OpenApiDocumentGeneratorSettings().AddCoreExConfiguration(o => o.JsonSerializerOptions = customOptions);

        ((SystemTextJsonSchemaGeneratorSettings)settings.SchemaSettings).SerializerOptions.Should().BeSameAs(customOptions);
    }

    [Test]
    public void AddCoreExConfiguration_NoConfigure_DefaultsToJsonDefaults()
    {
        var settings = new OpenApiDocumentGeneratorSettings().AddCoreExConfiguration();

        ((SystemTextJsonSchemaGeneratorSettings)settings.SchemaSettings).SerializerOptions.Should().BeSameAs(JsonDefaults.SerializerOptions);
    }
}
