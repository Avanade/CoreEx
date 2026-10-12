// #if implement-servicebus
global using CoreEx.Azure.Messaging.ServiceBus;
// #endif
// #if implement-cosmos
global using Microsoft.Azure.Cosmos;
// #endif
global using OpenTelemetry;
global using OpenTelemetry.Trace;
// #if implement-cosmos
global using System.Text.Json;
// #endif


namespace app-name.Relay;

public class Program
{
    private static void Main(string[] args)
    {
        // Create the web builder.
        var builder = WebApplication.CreateBuilder(args);

        // Add CoreEx host settings.
        builder.AddHostSettings();

        // Add authorization services - required for UseAuthorization() below; unlike the Api/Subscribe hosts, Relay has no AddControllers() to register this transitively.
        builder.Services.AddAuthorization();

        // Add CoreEx services.
        builder.Services
            .AddPrecisionTimeProvider()
            .AddExecutionContext()
            .AddMvcWebApi()
            .AddHttpWebApi()
            .AddHostedServiceManager();

        // Add the repository and related outbox relay services.
// #if implement-sqlserver
        builder.AddSqlServerClient("SqlServer");        // Adds the SqlServerClient (using Aspire library).
        builder.Services
            .AddSqlServerDatabase()                     // Adds the SqlServerDatabase.
            .AddSqlServerUnitOfWork()                   // Adds the SqlServerUnitOfWork for the SqlServerDatabase.
            .AddSqlServerOutboxRelay();                 // Adds the SqlServerOutboxRelay.

        builder.AddSqlServerOutboxRelayHostedService(); // Adds the SqlServerOutboxRelayHostedService.
// #elif implement-cosmos
        // Add the Cosmos DB client (Aspire); in Development, accept the local emulator's self-signed certificate and use Gateway mode.
        builder.AddAzureCosmosClient("Cosmos", configureClientOptions: o =>
        {
            o.UseSystemTextJsonSerializerWithOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

            if (builder.Environment.IsDevelopment())
            {
                o.ConnectionMode = ConnectionMode.Gateway;
                o.HttpClientFactory = () => new HttpClient(new HttpClientHandler { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator });
            }
        });

        builder.Services
            .AddCosmosDb("domain-name-lower")           // Adds the CosmosDb for the database.
            .AddCosmosDbHealthCheck();                  // Adds the CosmosDbHealthCheck; Aspire's AddAzureCosmosClient does not register one.

// #if refdata-enabled
        builder.AddCosmosDbOutboxRelayHostedService("ref-data");   // Adds the CosmosDbOutboxRelayHostedService(s) (Change Feed Processor) for the 'ref-data' container; add one call per outbox-hosting container.
// #endif
// #elif implement-postgres
        builder.AddAzureNpgsqlDataSource("Postgres");   // Adds the NpgsqlDataSource (using Aspire library).
        builder.Services
            .AddPostgresDatabase()                      // Adds the PostgresDatabase.
            .AddPostgresUnitOfWork()                    // Adds the PostgresUnitOfWork for the PostgresDatabase.
            .AddPostgresOutboxRelay();                  // Adds the PostgresOutboxRelay.

        builder.AddPostgresOutboxRelayHostedService();  // Adds the PostgresOutboxRelayHostedService.
// #endif

// #if implement-servicebus
        // Add the Azure Service Bus publisher.
        builder.AddAzureServiceBusClient("ServiceBus");        // Adds the Azure Service Bus client (using Aspire library).
        builder.Services.AddAzureServiceBusPublisher((_, c) => // Adds the Azure Service Bus as the IEventPublisher.
        {
            c.SessionIdStrategy = ServiceBusSessionStrategy.UsePartitionKeyConvertedToAnId;  // Use a partition-id as the session-id.
        });
// #endif

        // Post-configure all health-checks; adds the standard tags.
        builder.Services.PostConfigureAllHealthChecks();

        // Add OpenTelemetry tracing.
        builder.WithCoreExTelemetry()
// #if implement-sqlserver
            .WithCoreExSqlServerTelemetry()
// #elif implement-cosmos
            .WithCoreExCosmosDbTelemetry()
// #elif implement-postgres
            .WithCoreExPostgresTelemetry()
// #endif
// #if implement-servicebus
            .WithCoreExServiceBusTelemetry()
// #endif
            .UseOtlpExporter();

        // Build the application.
        var app = builder.Build();

        // Configure the pipeline/middleware (order is important).
        app.UseCoreExExceptionHandler();
        app.UseHttpsRedirection();
        // app.UseAuthentication();   // TODO: register an authentication scheme (builder.Services.AddAuthentication(...)) then uncomment.
        app.UseAuthorization();
        app.UseExecutionContext();

        app.MapHealthChecks();   // Secure by default: detailed endpoints are disabled unless explicitly enabled (HealthCheckOptions.AreDetailedEndpointsEnabled) and secured (detailedGroupConfigure, e.g. g => g.RequireAuthorization()); basic live/startup/ready checks stay anonymous for orchestrator probes.
        app.MapHostedServices(/* groupConfigure: g => g.RequireAuthorization() */);         // Pause/resume management endpoints are admin-only and must be secured.

        // Run the application.
        app.Run();
    }
}
