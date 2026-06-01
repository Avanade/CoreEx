using solution-name.Infrastructure.Repositories;
using app-name.Subscriber.Subscribers;

namespace app-name.Subscriber;

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
#if (refdata-enabled)
            .AddReferenceDataOrchestrator<ReferenceDataService>()
#endif
            .AddMvcWebApi()
            .AddHttpWebApi()
            .AddHostedServiceManager();

#if (refdata-enabled)
        // Add all the dynamically registered services.
        builder.Services.AddDynamicServicesUsing<ReferenceDataService, ReferenceDataRepository>();
#endif

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
        builder.AddSqlClientConnection("SqlServer");    // Adds the SqlClient connection (using Aspire library).
        builder.Services
            .AddSqlServerDatabase()                     // Adds the SqlServerDatabase.
            .AddSqlServerUnitOfWork()                   // Adds the SqlServerUnitOfWork for the SqlServerDatabase.
            .AddEventFormatter()                        // Adds the EventFormatter to enable message formatting for publishing.
            .AddSqlServerOutboxPublisher()              // Adds the SqlServerOutboxPublisher as the IEventPublisher.
            .AddDbContext<domain-nameDbContext>()       // Adds the standard EF DbContext.
            .AddEfDb<domain-nameEfDb>();                // Adds the CoreEx extended EF service.
#elif (implement-postgres)
        builder.AddNpgsqlDataSource("Postgres");        // Adds the NpgsqlDataSource (using Aspire library).
        builder.Services
            .AddPostgresDatabase()                      // Adds the PostgresDatabase.
            .AddPostgresUnitOfWork()                    // Adds the PostgresUnitOfWork for the PostgresDatabase.
            .AddEventFormatter()                        // Adds the EventFormatter to enable message formatting for publishing.
            .AddPostgresOutboxPublisher()               // Adds the PostgresOutboxPublisher as the IEventPublisher.
            .AddDbContext<domain-nameDbContext>()       // Adds the standard EF DbContext.
            .AddEfDb<domain-nameEfDb>();                // Adds the CoreEx extended EF service.
#endif

#if (implement-servicebus)
        // Add the Azure Service Bus client and subscribe wiring.
        builder.AddAzureServiceBusClient("ServiceBus"); // Adds the Azure Service Bus client (using Aspire library).

        // Add event formatter and subscribed-manager.
        builder.Services.AddSubscribedManager((_, c) => c.AddSubscribersUsing<XxxSubscriber>()); // TODO: update with one of your subscriber types.

        // Build and create the Azure Service Bus receiving services.
        builder.Services.AzureServiceBusReceiving()
            .WithSessionReceiver(_ =>
            {
                var o = ServiceBusSessionReceiverOptions.CreateForTopicSubscription(); // Set the topic and subscription from configuration.
                o.SessionProcessorOptions.MaxConcurrentSessions = 4;
                return o;
            })
            .WithSubscribedSubscriber()                 // Adds the service bus subscriber using the SubscribedManager.
            .WithHostedService()                        // Adds the service bus receiver as a hosted service.
            .Build();                                   // Builds all the services and adds to the service collection.
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
#if (implement-servicebus)
            .WithCoreExServiceBusTelemetry()
#endif
            .UseOtlpExporter();

        // Build the application.
        var app = builder.Build();

        // Configure the pipeline/middleware (order is important).
        app.UseCoreExExceptionHandler();
        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.UseExecutionContext();
        app.MapControllers();

        app.UseOpenApi();
        app.UseSwaggerUi();
        app.MapHealthChecks();
        app.MapHostedServices();

        // Run the application.
        app.Run();
    }
}
