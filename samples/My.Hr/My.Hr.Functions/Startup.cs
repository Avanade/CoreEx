using System.Threading.Tasks;
using CoreEx;
using CoreEx.AspNetCore.HealthChecks;
using CoreEx.AspNetCore.WebApis;
using CoreEx.Database;
using CoreEx.Database.HealthChecks;
using CoreEx.Http.HealthChecks;
using CoreEx.Json.Merge;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using My.Hr.Business;
using My.Hr.Business.Data;
using My.Hr.Business.External;
using My.Hr.Business.Services;
using Az = Azure.Messaging.ServiceBus;

[assembly: FunctionsStartup(typeof(My.Hr.Functions.Startup))]

namespace My.Hr.Functions;

public class Startup : FunctionsStartup
{
    public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
    {
    }

    public override void Configure(IFunctionsHostBuilder builder)
    {
        try
        {
            // Register the core services.
            builder.Services
                .AddSettings<HrSettings>()
                .AddReferenceDataOrchestrator<ReferenceDataService>()
                .AddExecutionContext()
                .AddJsonSerializer()
                .AddEventDataSerializer()
                .AddEventDataFormatter()
                .AddEventPublisher()
                .AddSingleton(sp => new Az.ServiceBusClient(sp.GetRequiredService<HrSettings>().ServiceBusConnection__fullyQualifiedNamespace))
                .AddAzureServiceBusSender()
                .AddWebApi((_, webapi) => webapi.UnhandledExceptionAsync = (ex, logger, _) => Task.FromResult(ex is DbUpdateConcurrencyException efex ? webapi.CreateActionResultFromExtendedException(new ConcurrencyException(), logger) : null))
                .AddJsonMergePatch(sp => new JsonMergePatch())
                .AddWebApiPublisher()
                .AddAzureServiceBusSubscriber();

            // Register the health checks.
            builder.Services
                .AddHealthChecks()
                .AddTypeActivatedCheck<TypedHttpClientCoreHealthCheck<GenderizeApiClient>>("Genderize API")
                .AddTypeActivatedCheck<TypedHttpClientCoreHealthCheck<AgifyApiClient>>("Agify API")
                .AddTypeActivatedCheck<TypedHttpClientCoreHealthCheck<NationalizeApiClient>>("Nationalize API")
                //.AddTypeActivatedCheck<AzureServiceBusQueueHealthCheck>("Health check for service bus verification queue", HealthStatus.Unhealthy, nameof(HrSettings.ServiceBusConnection), nameof(HrSettings.VerificationQueueName))
                .AddTypeActivatedCheck<DatabaseHealthCheck<IDatabase>>("SQL Server", HealthStatus.Unhealthy, tags: default, timeout: System.TimeSpan.FromSeconds(15), nameof(HrSettings.ConnectionStrings__Database));

            // Register the business services.
            builder.Services
                .AddScoped<ReferenceDataService>()
                .AddScoped<EmployeeService>()
                .AddScoped<VerificationService>()
                .AddFluentValidators<EmployeeService>();

            // Register the typed backend http clients.
            builder.Services.AddTypedHttpClient<AgifyApiClient>("Agify");
            builder.Services.AddTypedHttpClient<GenderizeApiClient>("Genderize");
            builder.Services.AddTypedHttpClient<NationalizeApiClient>("Nationalize");

            // Database
            builder.Services.AddDatabase<HrDb>();
            builder.Services.AddDbContext<HrDbContext>((sp, o) => o.UseSqlServer(sp.GetRequiredService<IDatabase>().GetConnection()));
        }
        catch (System.Exception ex)
        {
            // try catch block for running the function in docker container, without it, it may fail silently.
            System.Console.Error.WriteLine(ex);
            throw;
        }
    }
}