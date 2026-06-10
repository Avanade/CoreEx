using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Trace;
using StackExchange.Redis;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;
using solution-name.Infrastructure.Repositories;

namespace app-name.Api;

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
            .AddExecutionContext()
// #if (refdata-enabled)
            .AddReferenceDataOrchestrator<ReferenceDataService>()
// #endif
            .AddMvcWebApi()
            .AddHttpWebApi();

// #if (refdata-enabled)
        // Add all the dynamically registered services.
        builder.Services.AddDynamicServicesUsing<ReferenceDataService, ReferenceDataRepository>();
// #endif

        // Add L1/L2 caching services.
        builder.Services.AddMemoryCache();              // Adds the in-memory cache - L1.
        builder.AddRedisDistributedCache("redis");      // Adds Redis as the distributed cache - L2.

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

        // Add the repository and related database services.
<<<<<<< HEAD
// #if (implement-sqlserver)
=======
#if (implement-sqlserver)
>>>>>>> 9d0485ed6ca54a21eadbcd1a620214ee380e5905
        builder.AddSqlServerClient("SqlServer");        // Adds the SqlServerClient (using Aspire library).
        builder.Services
            .AddSqlServerDatabase()                     // Adds the SqlServerDatabase.
            .AddSqlServerUnitOfWork()                   // Adds the SqlServerUnitOfWork for the SqlServerDatabase.
            .AddEventFormatter()                        // Adds the EventFormatter to enable message formatting for publishing.
<<<<<<< HEAD
// #if (outbox-enabled)
            .AddSqlServerOutboxPublisher()              // Adds the SqlServerOutboxPublisher as the IEventPublisher.
// #endif
            .AddDbContext<domain-nameDbContext>()       // Adds the standard EF DbContext.
            .AddEfDb<domain-nameEfDb>();                // Adds the CoreEx extended EF service.
// #elif (implement-postgres)
        builder.AddNpgsqlDataSource("Postgres");        // Adds the NpgsqlDataSource (using Aspire library).
=======
#if (outbox-enabled)
            .AddSqlServerOutboxPublisher()              // Adds the SqlServerOutboxPublisher/IEventPublisher.
#endif
            .AddDbContext<domain-nameDbContext>()       // Adds the standard EF DbContext.
            .AddEfDb<domain-nameEfDb>();                // Adds the CoreEx extended EF service.
#elif (implement-postgres)
        builder.AddAzureNpgsqlDataSource("Postgres");   // Adds the NpgsqlDataSource (using Aspire library).
>>>>>>> 9d0485ed6ca54a21eadbcd1a620214ee380e5905
        builder.Services
            .AddPostgresDatabase()                      // Adds the PostgresDatabase.
            .AddPostgresUnitOfWork()                    // Adds the PostgresUnitOfWork for the PostgresDatabase.
            .AddEventFormatter()                        // Adds the EventFormatter to enable message formatting for publishing.
<<<<<<< HEAD
// #if (outbox-enabled)
            .AddPostgresOutboxPublisher()               // Adds the PostgresOutboxPublisher as the IEventPublisher.
// #endif
=======
#if (outbox-enabled)
            .AddPostgresOutboxPublisher()               // Adds the PostgresOutboxPublisher/IEventPublisher.
#endif
>>>>>>> 9d0485ed6ca54a21eadbcd1a620214ee380e5905
            .AddDbContext<domain-nameDbContext>()       // Adds the standard EF DbContext.
            .AddEfDb<domain-nameEfDb>();                // Adds the CoreEx extended EF service.
// #endif

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
// #if (implement-sqlserver)
            .WithCoreExSqlServerTelemetry()
// #elif (implement-postgres)
            .WithCoreExPostgresTelemetry()
// #endif
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
