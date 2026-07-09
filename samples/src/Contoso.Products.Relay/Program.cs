using CoreEx.Azure.Messaging.ServiceBus;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace Contoso.Products.Relay;

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

        // Add the repository and related outbox services.
        builder.AddAzureNpgsqlDataSource("Postgres");  // Adds the NpgsqlDataSource (using Aspire library).
        builder.Services
            .AddPostgresDatabase()                     // Adds the PostgresDatabase.
            .AddPostgresUnitOfWork()                   // Adds the PostgresUnitOfWork for the PostgresDatabase.
            .AddPostgresOutboxRelay();                 // Adds the PostgresOutboxRelay.

        // Adds the PostgresOutboxRelayHostedService.
        builder.AddPostgresOutboxRelayHostedService();

        // Add the Azure Service Bus services.
        builder.AddAzureServiceBusClient("ServiceBus"); // Adds azure service bus client using aspire.
        builder.Services.AddAzureServiceBusPublisher((_, c) =>  // Adds the service bus as the IEventPublisher.
        {
            c.SessionIdStrategy = ServiceBusSessionStrategy.UsePartitionKeyConvertedToAnId;  // Use a partition-id as the session-id.
        });

        // Post-configure all health-checks; adds the standard tags.
        builder.Services.PostConfigureAllHealthChecks();

        // Add OpenTelemetry tracing.
        builder.WithCoreExTelemetry()
            .WithCoreExPostgresTelemetry()
            .WithCoreExServiceBusTelemetry()
            .UseOtlpExporter();

        // Build the application.
        var app = builder.Build();

        // Configure the pipeline/middleware (order is important).
        app.UseCoreExExceptionHandler();
        app.UseHttpsRedirection();
        // app.UseAuthentication();   // TODO: register an authentication scheme (builder.Services.AddAuthentication(...)) then uncomment.
        // app.UseAuthorization();    // TODO: register authorization services (builder.Services.AddAuthorization(...)) then uncomment.
        app.UseExecutionContext();

        app.MapHealthChecks();
        app.MapHostedServices();

        // Run the application.
        app.Run();
    }
}
