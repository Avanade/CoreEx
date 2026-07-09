using CoreEx.Azure.Messaging.ServiceBus;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace Contoso.Shopping.Relay;

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
        builder.AddSqlServerClient("SqlServer");        // Adds the SqlServerClient (using Aspire library).
        builder.Services
            .AddSqlServerDatabase()                     // Adds the SqlServerDatabase.
            .AddSqlServerUnitOfWork()                   // Adds the SqlServerUnitOfWork for the SqlServerDatabase.
            .AddSqlServerOutboxRelay();                 // Adds the SqlServerOutboxRelay.

        // Adds the SqlServerOutboxRelayHostedService.
        builder.AddSqlServerOutboxRelayHostedService();

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
            .WithCoreExSqlServerTelemetry()
            .WithCoreExServiceBusTelemetry()
            .UseOtlpExporter();

        // Build the application.
        var app = builder.Build();

        // Configure the pipeline/middleware (order is important).
        app.UseCoreExExceptionHandler();
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseExecutionContext();

        app.MapHealthChecks();
        app.MapHostedServices();

        // Run the application.
        app.Run();
    }
}
