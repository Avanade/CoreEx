using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using CoreEx.TestFunction.Services;
using CoreEx.Functions;
using CoreEx.Json;
using CoreEx.Configuration;

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
                .AddScoped<IExecutor, Executor>()
                .AddScoped<SettingsBase, TestSettings>()
                .AddScoped<IJsonSerializer, CoreEx.Text.Json.JsonSerializer>()
                .AddScoped<IHttpTriggerExecutor, HttpTriggerExecutor>()
                .AddScoped<IServiceBusTriggerExecutor, ServiceBusTriggerExecutor>();

            // Register the typed backend http client.
            builder.Services.AddHttpClient<BackendHttpClient>("Backend", (sp, hc) =>
            {
                var settings = sp.GetService<TestSettings>();
                hc.BaseAddress = settings.BackendEndpoint;
            });

            // Register the underlying function services.
            builder.Services
                .AddAutoMapper(typeof(HttpTriggerService).Assembly)
                .AddScoped<HttpTriggerService>();
        }
    }
}