using CoreEx.Configuration;
using CoreEx.DependencyInjection;
using CoreEx.Events;
using CoreEx.Json;
using CoreEx.Text.Json;
using CoreEx.WebApis;
using CoreEx.Healthchecks;
using CoreEx.Healthchecks.Checks;

using My.Hr.Business;
using My.Hr.Business.Services;

using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using CoreEx.Messaging.Azure.ServiceBus;

[assembly: FunctionsStartup(typeof(My.Hr.Functions.Startup))]

namespace My.Hr.Functions;

public class Startup : FunctionsStartup
{
    public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
    {
    }

    public override void Configure(IFunctionsHostBuilder builder)
    {
        // Register the core services.
        builder.Services
            .AddSingleton<HrSettings>()
            .AddExecutionContext()
            .AddScoped<SettingsBase, HrSettings>()
            .AddScoped<IJsonSerializer, CoreEx.Text.Json.JsonSerializer>(_ => new CoreEx.Text.Json.JsonSerializer(new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
                WriteIndented = false,
                Converters = { new JsonStringEnumConverter(), new ExceptionConverterFactory() }
            }))
            .AddScoped<IEventSerializer, CoreEx.Text.Json.EventDataSerializer>()
            .AddScoped<IEventPublisher, NullEventPublisher>()
            .AddScoped<WebApi, WebApi>()
            .AddScoped<WebApiPublisher>()
            .AddScoped<ServiceBusSubscriber>();

        // // Register the health checks.
        builder.Services
            .AddScoped<HealthService>()
            .AddHealthChecks()
            .AddTypeActivatedCheck<TypedHttpClientCoreHealthCheck<GenderizeApiClient>>("Genderize API")
            .AddTypeActivatedCheck<TypedHttpClientCoreHealthCheck<AgifyApiClient>>("Agify API")
            .AddTypeActivatedCheck<TypedHttpClientCoreHealthCheck<NationalizeApiClient>>("Nationalize API");

        // Register the business services.
        builder.Services
            .AddScoped<ReferenceDataService>()
            .AddScoped<EmployeeService>();

        // Register the typed backend http clients.
        builder.Services.AddTypedHttpClient<AgifyApiClient>("Agify", (_, __) => { });
        builder.Services.AddTypedHttpClient<GenderizeApiClient>("Genderize", (_, __) => { });
        builder.Services.AddTypedHttpClient<NationalizeApiClient>("Nationalize", (_, __) => { });

        // Database
        builder.Services.AddDbContext<HrDbContext>(
            options => options.UseSqlServer("name=ConnectionStrings:Database"));
    }
}