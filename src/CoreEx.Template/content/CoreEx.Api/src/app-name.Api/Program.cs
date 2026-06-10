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
           .AddMvcWebApi()
           .AddHttpWebApi();

        // NOTE: Reference-data orchestrator and dynamic service registration are performed AFTER CodeGen runs.
        // See: BOOTSTRAP_PHASE_2.md in your project root for the post-CodeGen setup steps.
        // The following will be uncommented and moved here after running: dotnet run --project tools/app-name.CodeGen
        //
        // // #if (refdata-enabled)
        // builder.Services.AddReferenceDataOrchestrator<ReferenceDataService>();
        // builder.Services.AddDynamicServicesUsing<ReferenceDataService, ReferenceDataRepository>();
        // // #endif

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
#if (implement-sqlserver)
        builder.AddSqlServerClient("SqlServer");        // Adds the SqlServerClient (using Aspire library).
        builder.Services
           .AddSqlServerDatabase()                     // Adds the SqlServerDatabase.
           .AddSqlServerUnitOfWork()                   // Adds the SqlServerUnitOfWork for the SqlServerDatabase.
           .AddEventFormatter()                        // Adds the EventFormatter to enable message formatting for publishing.
#if (outbox-enabled)
           .AddSqlServerOutboxPublisher()              // Adds the SqlServerOutboxPublisher as the IEventPublisher.
#endif
           .AddDbContext<domain-nameDbContext>()       // Adds the standard EF DbContext.
           .AddEfDb<domain-nameEfDb>();                // Adds the CoreEx extended EF service.
#elif (implement-postgres)
        builder.AddNpgsqlDataSource("Postgres");        // Adds the NpgsqlDataSource (using Aspire library).
        builder.Services
            .AddPostgresDatabase()                      // Adds the PostgresDatabase.
            .AddPostgresUnitOfWork()                    // Adds the PostgresUnitOfWork for the PostgresDatabase.
            .AddEventFormatter()                        // Adds the EventFormatter to enable message formatting for publishing.
#if (outbox-enabled)
            .AddPostgresOutboxPublisher()               // Adds the PostgresOutboxPublisher as the IEventPublisher.
#endif
            .AddDbContext<domain-nameDbContext>()       // Adds the standard EF DbContext.
            .AddEfDb<domain-nameEfDb>();                // Adds the CoreEx extended EF service.
#endif

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
#if (implement-sqlserver)
            .WithCoreExSqlServerTelemetry()
#elif (implement-postgres)
            .WithCoreExPostgresTelemetry()
#endif
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
