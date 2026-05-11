using Contoso.Orders.Infrastructure.Repositories;
using Contoso.Order.Workflow.Client;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Trace;
using StackExchange.Redis;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;

namespace Contoso.Orders.Api;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddHostSettings();

        builder.Services
            .AddExecutionContext()
            .AddReferenceDataOrchestrator<ReferenceDataService>()
            .AddMvcWebApi()
            .AddHttpWebApi();

        builder.Services.AddContosoOrderWorkflowClient(builder.Configuration);

        builder.Services.AddDynamicServicesUsing<ReferenceDataService, ReferenceDataRepository>();

        builder.Services.AddMemoryCache();
        builder.AddRedisDistributedCache("redis");

        builder.Services.AddFusionCache()
            .WithRegisteredMemoryCache()
            .WithRegisteredDistributedCache()
            .WithBackplane(sp => new RedisBackplane(new RedisBackplaneOptions { Configuration = sp.GetRequiredService<IOptions<ConfigurationOptions>>().Value.ToString() }))
            .WithSystemTextJsonSerializer(JsonDefaults.SerializerOptions);

        builder.Services
            .AddFusionHybridCache()
            .AddDefaultCacheKeyProvider()
            .AddHybridCacheIdempotencyProvider();

        builder.AddSqlServerClient("SqlServer");
        builder.Services
            .AddSqlServerDatabase()
            .AddSqlServerUnitOfWork()
            .AddEventFormatter()
            .AddSqlServerOutboxPublisher<OrdersOutboxPublisher>()
            .AddDbContext<OrdersDbContext>()
            .AddEfDb<OrdersEfDb>();

        builder.Services.PostConfigureAllHealthChecks();

        builder.Services.AddControllers();

        builder.Services.AddOpenApiDocument(s =>
        {
            s.Title = builder.Environment.ApplicationName;
            s.AddCoreExConfiguration();
        });

        builder.WithCoreExTelemetry()
            .WithCoreExSqlServerTelemetry()
            .UseOtlpExporter();

        var app = builder.Build();

        app.UseCoreExExceptionHandler();
        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.UseExecutionContext();
        app.UseIdempotencyKey();
        app.MapControllers();

        app.UseOpenApi();
        app.UseSwaggerUi();
        app.MapHealthChecks();

        app.Run();
    }
}