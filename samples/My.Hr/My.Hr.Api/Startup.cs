using CoreEx.Azure.HealthChecks;
using CoreEx.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

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
        // Register the core services.
        services
            .AddSettings<HrSettings>()
            .AddReferenceDataOrchestrator(sp => new ReferenceDataOrchestrator(sp).Register<ReferenceDataService>())
            .AddExecutionContext()
            .AddJsonSerializer()
            .AddEventDataSerializer()
            .AddEventDataFormatter()
            .AddEventPublisher()
            .AddAzureServiceBusSender()
            .AddAzureServiceBusPurger()
            .AddAzureServiceBusClient(connectionName: nameof(HrSettings.ServiceBusConnection))
            .AddJsonMergePatch()
            .AddWebApi(c => c.UnhandledExceptionAsync = (ex, _) => Task.FromResult(ex is DbUpdateConcurrencyException efex ? new ConcurrencyException().ToResult() : null))
            .AddReferenceDataContentWebApi()
            .AddRequestCache();

        // Register the business services.
        services
            .AddScoped<ReferenceDataService>()
            .AddScoped<IEmployeeService, EmployeeService>()
            .AddFluentValidators<EmployeeService>();

        // Register the database and EF services, including required AutoMapper.
        services.AddDatabase(sp => new HrDb(sp.GetRequiredService<HrSettings>()))
                .AddDbContext<HrDbContext>((sp, o) => o.UseSqlServer(sp.GetRequiredService<IDatabase>().GetConnection()))
                .AddEfDb<HrEfDb>()
                .AddAutoMapper(typeof(HrEfDb).Assembly)
                .AddAutoMapperWrapper();

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
        => app
           .UseWebApiExceptionHandler()
           .UseSwagger()
           .UseSwaggerUI()
           .UseHttpsRedirection()
           .UseRouting()
           .UseAuthorization()
           .UseExecutionContext()
           .UseEndpoints(endpoints => endpoints.MapControllers());
}