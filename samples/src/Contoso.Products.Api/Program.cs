using Contoso.Products.Infrastructure.Repositories;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Trace;
using StackExchange.Redis;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;

namespace Contoso.Products.Api;

public class Program
{
    private static void Main(string[] args)
    {
        // Create the web builder.
        var builder = WebApplication.CreateBuilder(args);

        // Add CoreEx host settings.
        builder.AddHostSettings();

        // Add CoreEx services.
        builder.Services
            .AddPrecisionTimeProvider()
            .AddExecutionContext()
            .AddReferenceDataOrchestrator<ReferenceDataService>()
            .AddMvcWebApi()
            .AddHttpWebApi();

        // Add all the dynamically registered services.
        builder.Services.AddDynamicServicesUsing<ReferenceDataService, ReferenceDataRepository>();

        // Add L1/L2 caching services.
        builder.Services.AddMemoryCache();              // Adds the in-memory cache - L1.
        builder.AddRedisDistributedCache("redis");      // Adds Redis as the distributed cache (using Aspire library) - L2.

        // Add and wire-up FusionCache including backplane.
        builder.Services.AddFusionCache()               
            .WithRegisteredMemoryCache()
            .WithRegisteredDistributedCache()
            .WithBackplane(sp => new RedisBackplane(new RedisBackplaneOptions { Configuration = sp.GetRequiredService<IOptions<ConfigurationOptions>>().Value.ToString() }))
            .WithSystemTextJsonSerializer(JsonDefaults.SerializerOptions);

        // Add CoreEx caching services.
        builder.Services
            .AddFusionHybridCache()                     // Adds the CoreEx.Caching.IHybridCache for FusionCache.
            .AddDefaultCacheKeyProvider()               // Adds the default CoreEx.Caching.ICacheKeyProvider.
            .AddHybridCacheIdempotencyProvider();       // Adds the CoreEx.Caching.Idempotency.IIdempotencyProvider.

        // Add the repository and related outbox services.
        builder.AddSqlServerClient("SqlServer");        // Adds the SqlServerClient (using Aspire library).
        builder.Services
            .AddSqlServerDatabase()                     // Adds the SqlServerDatabase.
            .AddSqlServerUnitOfWork()                   // Adds the SqlServerUnitOfWork for the SqlServerDatabase.
            .AddEventFormatter()                        // Adds the EventFormatter to enable message formatting for publishing.
            .AddSqlServerOutboxPublisher()              // Adds the SqlServerOutboxPublisher/IEventPublisher.
            .AddDbContext<ProductsDbContext>()          // Adds the standard EF DbContext.
            .AddEfDb<ProductsEfDb>();                   // Adds the CoreEx extended EF service.

        // Post-configure all health-checks; adds the standard tags.
        builder.Services.PostConfigureAllHealthChecks();

        // Add the ASP.NET Core services.
        builder.Services.AddControllers();

        // Add the OpenAPI services.
        builder.Services.AddOpenApiDocument(s =>
        {
            s.Title = builder.Environment.ApplicationName;
            s.AddCoreExConfiguration();
        });

        // Add OpenTelemetry tracing.
        builder.WithCoreExTelemetry()
            .WithCoreExSqlServerTelemetry()
            .UseOtlpExporter();

        // Build the application.
        var app = builder.Build();

        // Configure the pipeline/middleware (order is important).
        app.UseCoreExExceptionHandler();
        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.UseExecutionContext();
        app.UseIdempotencyKey();
        app.MapControllers();

        app.UseOpenApi();
        app.UseSwaggerUi();
        app.MapHealthChecks();

        // Run the application.
        app.Run();
    }
}