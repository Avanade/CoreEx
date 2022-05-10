using CoreEx;
using CoreEx.HealthChecks;
using CoreEx.Azure.HealthChecks;
using CoreEx.RefData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using My.Hr.Business;
using My.Hr.Business.Data;
using My.Hr.Business.Services;
using System.Reflection;
using CoreEx.WebApis;

namespace My.Hr.Api;

public class Startup
{
    // todo: add azure app configuration (conditional?)

    /// <summary>
    /// The configure services method called by the runtime; use this method to add services to the container.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    public void ConfigureServices(IServiceCollection services)
    {
        // Add services to the container.
        // Register the core services.
        services
            .AddSettings<HrSettings>()
            .AddReferenceDataOrchestrator(sp => new ReferenceDataOrchestrator(sp, new MemoryCache(new MemoryCacheOptions())).Register<ReferenceDataService>())
            .AddExecutionContext()
            .AddJsonSerializer()
            .AddEventDataSerializer()
            .AddEventDataFormatter()
            .AddEventPublisher()
            .AddAzureServiceBusSender()
            .AddAzureServiceBusClient(connectionName: nameof(HrSettings.ServiceBusConnection))
            .AddJsonMergePatch()
            .AddWebApi(c => c.OnUnhandledException = (ex, _) => Task.FromResult(ex is DbUpdateConcurrencyException efex ? new ConcurrencyException().ToResult() : null));

        // Register the business services.
        services
            .AddScoped<ReferenceDataService>()
            .AddScoped<EmployeeService>();

        // Database
        services.AddDbContext<HrDbContext>((sp, o) => o.UseSqlServer(sp.GetRequiredService<IConfiguration>().GetConnectionString("Database")));

        // Register the health checks.
        services
            .AddScoped<HealthService>()
            .AddHealthChecks()
            .AddTypeActivatedCheck<AzureServiceBusQueueHealthCheck>("Health check for service bus verification queue", HealthStatus.Unhealthy, nameof(HrSettings.ServiceBusConnection), nameof(HrSettings.VerificationQueueName));

        services.AddControllers();

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
            options.OperationFilter<AcceptsBodyOperationFilter>();  // Needed to support AcceptsBodyAttribue where body parameter not explicitly defined.
        });
    }

    /// <summary>
    /// The configure method called by the runtime; use this method to configure the HTTP request pipeline.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
    public void Configure(IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}