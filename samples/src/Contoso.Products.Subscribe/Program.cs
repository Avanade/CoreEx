using Contoso.Products.Application;
using Contoso.Products.Infrastructure.Repositories;
using Contoso.Products.Subscribe.Subscribers;
using CoreEx.Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Trace;
using StackExchange.Redis;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;

namespace Contoso.Products.Subscribe;

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
            .AddHttpWebApi()
            .AddHostedServiceManager();

        // Add all the dynamically registered services.
        builder.Services.AddDynamicServicesUsing<ReservationConfirmSubscriber, ReferenceDataService, ReferenceDataRepository>();

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
        builder.AddAzureNpgsqlDataSource("Postgres");    // Adds the NpgsqlDataSource (using Aspire library).
        builder.Services
            .AddPostgresDatabase()                      // Adds the PostgresDatabase.
            .AddPostgresUnitOfWork()                    // Adds the PostgresUnitOfWork for the PostgresDatabase.
            .AddEventFormatter()                        // Adds the EventFormatter to enable message formatting for publishing.
            .AddPostgresOutboxPublisher()               // Adds the ProductsOutboxPublisher as the PostgresOutboxPublisher/IEventPublisher.
            .AddDbContext<ProductsDbContext>()          // Adds the standard EF DbContext.
            .AddEfDb<ProductsEfDb>();                   // Adds the CoreEx extended EF service.

        // Add Azure Service Bus client using Aspire.
        builder.AddAzureServiceBusClient("ServiceBus");

        // Add event formatter and subscribed-manager.
        builder.Services.AddSubscribedManager((_, c) => c.AddSubscribersUsing<ReservationConfirmSubscriber>()); // Adds the SubscribedManager and dynamically links to the individual Subscribers.

        // Builds and creates the Azure Service Bus receiving services.
        builder.Services.AzureServiceBusReceiving()                                      
            .WithSessionReceiver(_ =>
            {                                                                                          // Adds the service bus receiver to pump messages to the subscriber.
                var o = ServiceBusSessionReceiverOptions.CreateForTopicSubscription();                 // Set the topic and subscription from configuration. 
                o.SessionProcessorOptions.MaxConcurrentSessions = 4;                                   // Set the maximum number of concurrent sessions to process.
                return o;
            })   
            .WithSubscribedSubscriber()                                                                // Adds the service bus subscriber using the ^ SubscribedManager.
            .WithHostedService()                                                                       // Adds the ^ service bus receiver as a hosted service.
            .Build();                                                                                  // Builds all the ^ services and adds to the service collection.

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
            .WithCoreExServiceBusTelemetry()
            .WithCoreExPostgresTelemetry()
            .UseOtlpExporter();

        // Build the application.
        var app = builder.Build();

        // Configure the pipeline/middleware (order is important).
        app.UseCoreExExceptionHandler();
        app.UseHttpsRedirection();
        // app.UseAuthentication();   // TODO: register an authentication scheme (builder.Services.AddAuthentication(...)) then uncomment.
        app.UseAuthorization();
        app.UseExecutionContext();
        app.MapControllers();

        app.UseOpenApi();
        app.UseSwaggerUi();
        app.MapHealthChecks(detailedGroupConfigure: g => g.RequireAuthorization());   // Detailed endpoints expose diagnostics and must be secured; basic live/startup/ready checks stay anonymous for orchestrator probes.
        app.MapHostedServices(groupConfigure: g => g.RequireAuthorization());         // Pause/resume management endpoints are admin-only and must be secured.

        // Run the application.
        app.Run();
    }
}
