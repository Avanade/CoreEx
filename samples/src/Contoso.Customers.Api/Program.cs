using OpenTelemetry;
using OpenTelemetry.Trace;
using ZiggyCreatures.Caching.Fusion;

namespace Contoso.Customers.Api;

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
        builder.Services.AddDynamicServicesUsing<ReferenceDataService, CustomerRepository>();

        // Add caching services - in-memory (L1) only for this sample; no distributed (L2)/Redis, kept deliberately simple.
        builder.Services.AddMemoryCache();
        builder.Services.AddFusionCache()
            .WithRegisteredMemoryCache()
            .WithSystemTextJsonSerializer(JsonDefaults.SerializerOptions);

        builder.Services
            .AddFusionHybridCache()                      // Adds the CoreEx.Caching.IHybridCache for FusionCache.
            .AddDefaultCacheKeyProvider()                // Adds the default CoreEx.Caching.ICacheKeyProvider.
            .AddHybridCacheIdempotencyProvider();        // Adds the CoreEx.Caching.Idempotency.IIdempotencyProvider.

        // Add the Cosmos DB client (Aspire) - in Development, configured to accept the local emulator's self-signed certificate and use Gateway mode, matching CosmosTestBase's own approach.
        builder.AddAzureCosmosClient("Cosmos", configureClientOptions: o =>
        {
            o.UseSystemTextJsonSerializerWithOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

            if (builder.Environment.IsDevelopment())
            {
                o.ConnectionMode = ConnectionMode.Gateway;
                o.HttpClientFactory = () => new HttpClient(new HttpClientHandler { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator });
            }
        });

        // Add the Cosmos repository and related outbox services.
        builder.Services.AddCosmosDb<CustomersCosmosDb>("contoso");
        builder.Services
            .AddEventFormatter()                         // Adds the EventFormatter to enable message formatting for publishing.
            .AddCosmosDbEventPublisher()                 // Adds the CosmosDbEventPublisher/IEventPublisher
            .AddCosmosDbUnitOfWork()                     // Adds the CosmosDbUnitOfWork/IUnitOfWork, matching AddPostgresUnitOfWork/AddSqlServerUnitOfWork's multi-register shape.
            .AddCosmosDbHealthCheck();                   // Adds the CosmosDbHealthCheck - Aspire's own AddAzureCosmosClient does not register one itself, unlike its Npgsql/SqlClient counterparts.

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
            .WithCoreExCosmosDbTelemetry()
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

        app.UseOpenApi();
        app.UseSwaggerUi();
        app.MapHealthChecks();   // Secure by default: detailed endpoints are disabled unless explicitly enabled (HealthCheckOptions.AreDetailedEndpointsEnabled) and secured (detailedGroupConfigure, e.g. g => g.RequireAuthorization()); basic live/startup/ready checks stay anonymous for orchestrator probes.

        // Run the application.
        app.Run();
    }
}
