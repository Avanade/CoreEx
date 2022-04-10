using CoreEx.Configuration;
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
using CoreEx.Messaging.Azure.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;

[assembly: FunctionsStartup(typeof(My.Hr.Functions.Startup))]

namespace My.Hr.Functions;

public class Startup : FunctionsStartup
{
    public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
    {
    }

    public override void Configure(IFunctionsHostBuilder builder)
    {
        // try-catch to capture exceptions during startup when running in docker container
        try
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
                .AddScoped<IEventPublisher, EventPublisher>()
                .AddScoped<EventDataFormatter>()
                .AddScoped<IEventSender, ServiceBusSender>()
                .AddScoped<WebApi, WebApi>()
                .AddScoped<WebApiPublisher>()
                .AddScoped<ServiceBusSubscriber>()
                .AddAzureServiceBusClient(connectionName: nameof(HrSettings.ServiceBusConnection));

            // Register the health checks.
            builder.Services
                .AddScoped<HealthService>()
                .AddHealthChecks()
                .AddTypeActivatedCheck<TypedHttpClientCoreHealthCheck<GenderizeApiClient>>("Genderize API")
                .AddTypeActivatedCheck<TypedHttpClientCoreHealthCheck<AgifyApiClient>>("Agify API")
                .AddTypeActivatedCheck<TypedHttpClientCoreHealthCheck<NationalizeApiClient>>("Nationalize API")
                .AddTypeActivatedCheck<AzureServiceBusQueueHealthCheck>("Health check for service bus verification queue", HealthStatus.Unhealthy, nameof(HrSettings.ServiceBusConnection), nameof(HrSettings.VerificationQueueName))
                .AddTypeActivatedCheck<AzureServiceBusQueueHealthCheck>("Health check for service bus verification results queue", HealthStatus.Unhealthy, nameof(HrSettings.ServiceBusConnection), nameof(HrSettings.VerificationResultsQueueName));

            // Register the business services.
            builder.Services
                .AddScoped<ReferenceDataService>()
                .AddScoped<EmployeeService>()
                .AddScoped<VerificationService>();

            // Register the typed backend http clients.
            builder.Services.AddTypedHttpClient<AgifyApiClient>("Agify");
            builder.Services.AddTypedHttpClient<GenderizeApiClient>("Genderize");
            builder.Services.AddTypedHttpClient<NationalizeApiClient>("Nationalize");

            // Database
            builder.Services.AddDbContext<HrDbContext>(
                options => options.UseSqlServer("name=ConnectionStrings:Database"));
        }
        catch (System.Exception ex)
        {
            System.Console.Error.WriteLine(ex);
            throw;
        }

    }
}