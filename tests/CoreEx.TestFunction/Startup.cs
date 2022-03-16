using CoreEx.Configuration;
using CoreEx.DependencyInjection;
using CoreEx.Events;
using CoreEx.Messaging.Azure.ServiceBus;
using CoreEx.Json;
using CoreEx.TestFunction.Services;
using CoreEx.WebApis;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

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
                .AddSingleton<TestSettings>()
                .AddExecutionContext()
                .AddScoped<SettingsBase, TestSettings>()
                .AddScoped<IJsonSerializer, CoreEx.Text.Json.JsonSerializer>()
                .AddScoped<IEventSerializer, CoreEx.Text.Json.EventDataSerializer>()
                // .AddScoped<IJsonSerializer, CoreEx.Newtonsoft.Json.JsonSerializer>()
                // .AddScoped<IEventSerializer, CoreEx.Newtonsoft.Json.EventDataSerializer>()
                // replace by your own implementation of IEventPublisher to send events to e.g. service bus
                .AddScoped<IEventPublisher, NullEventPublisher>()
                .AddScoped<WebApi, WebApi>()
                .AddScoped<WebApiPublisher, WebApiPublisher>()
                .AddScoped<ServiceBusSubscriber>();

            // Register the health checks.
            builder.Services
                .AddScoped<HealthService>()
                .AddHealthChecks();

            // Register the typed backend http client.
            builder.Services.AddTypedHttpClient<BackendHttpClient>("Backend", (sp, hc) =>
            {
                var settings = sp.GetService<TestSettings>();
                hc.BaseAddress = settings.BackendBaseAddress;
            });

            // Register the underlying function services.
            builder.Services
                .AddAutoMapper(typeof(ProductService).Assembly)
                .AddScoped<ProductService>();
        }
    }
}