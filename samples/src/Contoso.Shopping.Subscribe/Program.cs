using Contoso.Shopping.Application;
using Contoso.Shopping.Infrastructure.Clients;
using Contoso.Shopping.Infrastructure.Repositories;
using Contoso.Shopping.Subscribe.Subscribers;
using CoreEx.Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Trace;
using StackExchange.Redis;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;

namespace Contoso.Shopping.Subscribe;

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
        builder.Services.AddDynamicServicesUsing<ProductModifySubscriber, ReferenceDataService, ReferenceDataRepository>();

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
            .AddSqlServerOutboxPublisher()              // Adds the SqlServerOutboxPublisher for the SqlServerUnitOfWork.
            .AddDbContext<ShoppingDbContext>()          // Adds the standard  EF DbContext.
            .AddEfDb<ShoppingEfDb>();                   // Adds the CoreEx extended EF service.

        // Add Azure Service Bus client using Aspire.
        builder.AddAzureServiceBusClient("ServiceBus");
        builder.Services.AddAzureServiceBusPublisher((_, c) =>  // Adds the service bus as the IEventPublisher.
        {
            c.SessionIdStrategy = ServiceBusSessionStrategy.UsePartitionKeyConvertedToAnId;  // Use a partition-id as the session-id.
        }, addAsDefaultIEventPublisher: false);

        // Add event formatter and subscribed-manager.
        builder.Services
            .AddEventFormatter()                                                               // Adds the EventFormatter to enable message parsing.
            .AddSubscribedManager((_, c) => c.AddSubscribersUsing<ProductModifySubscriber>()); // Adds the SubscribedManager and dynamically links to the individual Subscribers.

        // Creates the Azure Service Bus receiving services builder.
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

        // Add external API services.
        builder.AddTypedHttpClient<ProductsHttpClient>("ProductsApi");

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
            .WithCoreExSqlServerTelemetry()
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
