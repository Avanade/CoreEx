using CoreEx.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using OpenTelemetry.Trace;
using Az = Azure.Messaging.ServiceBus;
using CoreEx.Database.HealthChecks;
using CoreEx.Azure.ServiceBus.HealthChecks;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using CoreEx.Json.Merge;

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
            .AddSingleton(sp => new Az.ServiceBusClient(sp.GetRequiredService<HrSettings>().ServiceBusConnection__fullyQualifiedNamespace))
            .AddAzureServiceBusSender()
            .AddAzureServiceBusPurger()
            .AddJsonMergePatch()
            .AddWebApi((_, webapi) => webapi.UnhandledExceptionAsync = (ex, _, _) => Task.FromResult(ex is DbUpdateConcurrencyException efex ? webapi.CreateActionResultFromExtendedException(new ConcurrencyException()) : null))
            .AddReferenceDataContentWebApi()
            .AddRequestCache();

        // Register the business services.
        services
            .AddScoped<ReferenceDataService>()
            .AddScoped<IEmployeeService, EmployeeService>()
            .AddScoped<IEmployeeResultService, EmployeeResultService>()
            .AddFluentValidators<EmployeeService>();

        // Register the database and EF services, including required AutoMapper.
        services.AddDatabase<HrDb>()
                .AddDbContext<HrDbContext>((sp, o) => o.UseSqlServer(sp.GetRequiredService<IDatabase>().GetConnection()))
                .AddEfDb<IHrEfDb, HrEfDb>()
                .AddAutoMapper(typeof(HrEfDb).Assembly)
                .AddAutoMapperWrapper();

        // Register the health checks.
        services
            .AddHealthChecks()
            .AddServiceBusReceiverHealthCheck(sp => sp.GetRequiredService<Az.ServiceBusClient>().CreateReceiver(sp.GetRequiredService<HrSettings>().VerificationQueueName), "verification-queue");

        services.AddControllers();

        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
        {
            services.AddOpenTelemetry().UseAzureMonitor();
            //services.Configure<AspNetCoreInstrumentationOptions>(options => options.RecordException = true);
            services.ConfigureOpenTelemetryTracerProvider((sp, builder) => builder.AddSource("CoreEx.*"));
        }

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
            options.OperationFilter<AcceptsBodyOperationFilter>();  // Needed to support AcceptsBodyAttribue where body parameter not explicitly defined.
            options.OperationFilter<PagingOperationFilter>(PagingOperationFilterFields.SkipTake);  // Needed to support PagingAttribute where PagingArgs parameter not explicitly defined.
            options.OperationFilter<QueryOperationFilter>(QueryOperationFilterFields.FilterAndOrderby);  // Needed to support QueryAttribute where QueryArgs parameter not explicitly defined.
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
           .UseHealthChecks("/healthz", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { ResponseWriter = HealthReportStatusWriter.WriteJsonResults })
           .UseReferenceDataOrchestrator()
           .UseEndpoints(endpoints => endpoints.MapControllers());
}