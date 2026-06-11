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
        builder.AddNpgsqlDataSource("Postgres");        // Adds the NpgsqlDataSource (using Aspire library).
        builder.Services
            .AddPostgresDatabase()                      // Adds the PostgresDatabase.
            .AddPostgresUnitOfWork()                    // Adds the PostgresUnitOfWork for the PostgresDatabase.
            .AddPostgresOutboxRelay();                  // Adds the PostgresOutboxRelay.

        builder.AddPostgresOutboxRelayHostedService();  // Adds the PostgresOutboxRelayHostedService.
// #endif

// #if implement-servicebus
        // Add the Azure Service Bus publisher.
        builder.AddAzureServiceBusClient("ServiceBus"); // Adds the Azure Service Bus client (using Aspire library).
        builder.Services.AddAzureServiceBusPublisher(); // Adds the Azure Service Bus as the IEventPublisher.
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
        app.UseExecutionContext();

        app.MapHealthChecks();
        app.MapHostedServices();

        // Run the application.
        app.Run();
    }
}
