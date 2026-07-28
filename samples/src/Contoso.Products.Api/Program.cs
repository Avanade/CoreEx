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
            .AddReferenceDataOrchestrator()
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
        builder.AddAzureNpgsqlDataSource("Postgres");   // Adds the NpgsqlDataSource (using Aspire library).
        builder.Services
            .AddPostgresDatabase()                      // Adds the PostgresDatabase.
            .AddPostgresUnitOfWork()                    // Adds the PostgresUnitOfWork for the PostgresDatabase.
            .AddEventFormatter()                        // Adds the EventFormatter to enable message formatting for publishing.
            .AddPostgresOutboxPublisher()               // Adds the PostgresOutboxPublisher/IEventPublisher.
            .AddDbContext<ProductsDbContext>()          // Adds the standard EF DbContext.
            .AddEfDb<ProductsEfDb>();                   // Adds the CoreEx extended EF service.

        // Post-configure all health-checks; adds the standard tags.
        builder.Services.PostConfigureAllHealthChecks();

        // Add the ASP.NET Core services.
        builder.Services.AddControllers();

        // Add the CoreEx GraphQL-lite bridge, exposing "products"/"product" roots over the existing QueryAsync/GetAsync pipeline.
        builder.Services.AddCoreExGraphQLLite((o, sp) =>
        {
            o.EnableIntrospection = true; // Defaults to false (secure-by-default); enabled here so client tooling (Postman, GraphiQL, etc.) can introspect the schema.

            o.AddQuery<ProductLite>("products", ProductQueryArgsConfig.Default, async (qa, pa, ct) => await CoreEx.ExecutionContext.GetRequiredService<IProductReadService>().QueryAsync(qa, pa, ct).ConfigureAwait(false))
             .AddGet<Product>("product", (args, ct) => CoreEx.ExecutionContext.GetRequiredService<IProductReadService>().GetAsync(args.GetIdentifier<string>(), ct));
        });

        // Add the OpenAPI services.
        builder.Services.AddOpenApiDocument(s =>
        {
            s.Title = builder.Environment.ApplicationName;
            s.AddCoreExConfiguration();
        });

        // Add OpenTelemetry tracing.
        builder.WithCoreExTelemetry()
            .WithCoreExPostgresTelemetry()
            .WithCoreExGraphQLTelemetry()
            .UseOtlpExporter();

        // Build the application.
        var app = builder.Build();

        // Configure the pipeline/middleware (order is important).
        app.UseCoreExExceptionHandler();
        app.UseHttpsRedirection();
        // app.UseAuthentication();   // TODO: register an authentication scheme (builder.Services.AddAuthentication(...)) then uncomment.
        app.UseAuthorization();
        app.UseExecutionContext();
        app.UseIdempotencyKey();
        app.MapControllers();
        app.MapCoreExGraphQLLite("/api/query" /* , configure: rb => rb.RequireAuthorization() */); // Additive GraphQL-lite bridge alongside the existing REST endpoints; unlike the REST
                                                                                                     // controllers above, this endpoint has no authorization applied by default - it can reach
                                                                                                     // the same underlying data, so uncomment 'configure' once an authentication scheme is
                                                                                                     // registered (see UseAuthentication() above).

        app.UseOpenApi();
        app.UseSwaggerUi();
        app.MapHealthChecks(/* detailedGroupConfigure: g => g.RequireAuthorization() */);   // Detailed endpoints expose diagnostics and must be secured; basic live/startup/ready checks stay anonymous for orchestrator probes.

        // Run the application.
        app.Run();
    }
}
