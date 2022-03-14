using CoreEx.AspNetCore;
using CoreEx.Configuration;
using CoreEx.DependencyInjection;
using CoreEx.Events;
using CoreEx.Functions;
using CoreEx.Json;
using CoreEx.TestFunction.Services;
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
                .AddScoped<IEventPublisherBase, NullEventPublisher>()
                .AddScoped<WebApi, WebApi>()
                .AddScoped<IHttpTriggerExecutor, HttpTriggerExecutor>()
                .AddScoped<IServiceBusTriggerExecutor, ServiceBusTriggerExecutor>();

            // Register the typed backend http client.
            builder.Services.AddHttpClient<BackendHttpClient>("Backend", (sp, hc) =>
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