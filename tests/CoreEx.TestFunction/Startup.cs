using CoreEx.Azure.ServiceBus;
using CoreEx.TestFunction.Services;
using CoreEx.AspNetCore.HealthChecks;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using CoreEx.Events.Subscribing;
using CoreEx.TestFunction.Subscribers;
using CoreEx.AspNetCore.WebApis;
using CoreEx.Http.HealthChecks;

[assembly: FunctionsStartup(typeof(CoreEx.TestFunction.Startup))]

namespace CoreEx.TestFunction
{
    public class Startup : FunctionsStartup
    {
        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
        }

        public override void Configure(IFunctionsHostBuilder builder)
        {
            // Register the core services.
            builder.Services
                .AddSettings<TestSettings>()
                .AddExecutionContext()
                .AddJsonSerializer()
                //.AddNewtonsoftJsonSerializer()
                .AddEventDataSerializer()
                //.AddCloudEventSerializer()
                //.AddNewtonsoftEventDataSerializer()
                //.AddNewtonsoftCloudEventSerializer()
                //.AddScoped<IEventSerializer, CoreEx.Text.Json.EventDataSerializer>()
                // replace by your own implementation of IEventPublisher to send events to e.g. service bus
                .AddNullEventPublisher()
                .AddScoped<WebApi, WebApi>()
                .AddJsonMergePatch()
                .AddScoped<WebApiPublisher, WebApiPublisher>()
                .AddScoped<ServiceBusSubscriber>();

            // Register orchestrated.
            builder.Services
                .AddEventSubscribers<NoValueSubscriber>()
                .AddEventSubscriberOrchestrator((_, eso) => eso.AddSubscribers(EventSubscriberOrchestrator.GetSubscribers<NoValueSubscriber>()))
                .AddAzureServiceBusOrchestratedSubscriber();

            // Register the health checks.
            builder.Services
                .AddHealthChecks()
                .AddTypeActivatedCheck<TypedHttpClientCoreHealthCheck<BackendHttpClient>>("Backend");

            // Register the typed backend http client.
            builder.Services.AddTypedHttpClient<BackendHttpClient>("Backend", (sp, hc) =>
            {
                var settings = sp.GetRequiredService<TestSettings>();
                hc.BaseAddress = settings.BackendBaseAddress;
            });

            // Register the underlying function services.
            builder.Services
                .AddAutoMapper(typeof(ProductService).Assembly)
                .AddScoped<ProductService>();
        }
    }
}