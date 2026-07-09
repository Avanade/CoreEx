// #if implement-servicebus
global using CoreEx.Azure.Messaging.ServiceBus;
// #endif
global using OpenTelemetry;
global using OpenTelemetry.Trace;


namespace app-name.Relay;

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
        // app.UseAuthorization();    // TODO: register authorization services (builder.Services.AddAuthorization(...)) then uncomment.
        app.UseExecutionContext();

        app.MapHealthChecks(detailedGroupConfigure: g => g.RequireAuthorization());   // Detailed endpoints expose diagnostics and must be secured; basic live/startup/ready checks stay anonymous for orchestrator probes.
        app.MapHostedServices(groupConfigure: g => g.RequireAuthorization());         // Pause/resume management endpoints are admin-only and must be secured.

        // Run the application.
        app.Run();
    }
}
