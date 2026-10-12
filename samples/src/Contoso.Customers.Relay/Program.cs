namespace Contoso.Customers.Relay;

/// <summary>
/// Relays the Customers domain's committed Cosmos outbox events to Azure Service Bus.
/// </summary>
public class Program
{
    /// <summary>
    /// Configures and runs the relay host.
    /// </summary>
    private static void Main(string[] args)
    {
        // Use the live host clock, not an ambient request's frozen Runtime timestamp; existing checkpoints still take precedence.
        var startTime = TimeProvider.System.GetUtcNow().UtcDateTime;
        var builder = WebApplication.CreateBuilder(args);
        builder.AddHostSettings();

        builder.Services
            .AddPrecisionTimeProvider()
            .AddExecutionContext()
            .AddMvcWebApi()
            .AddHttpWebApi()
            .AddHostedServiceManager();

        builder.AddAzureCosmosClient("Cosmos", configureClientOptions: o =>
        {
            o.UseSystemTextJsonSerializerWithOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            if (builder.Environment.IsDevelopment())
            {
                o.ConnectionMode = ConnectionMode.Gateway;
                o.HttpClientFactory = () => new HttpClient(new HttpClientHandler { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator });
            }
        });
        builder.Services.SuppressCosmosClientBackgroundInstrumentation();

        builder.Services.AddCosmosDb("contoso").AddCosmosDbHealthCheck();
        foreach (var containerId in new[] { "customers", "ref-data" })
        {
            builder.AddCosmosDbOutboxRelayHostedService(containerId, configureOptions: (_, o) =>
            {
                o.StartTime = startTime;
                o.PollInterval = builder.Configuration.GetValue<TimeSpan?>($"CoreEx:Host:Services:CosmosOutboxRelay:{containerId}:PollInterval");
                o.BatchSize = builder.Configuration.GetValue<int?>($"CoreEx:Host:Services:CosmosOutboxRelay:{containerId}:BatchSize");
            });
        }

        builder.AddAzureServiceBusClient("ServiceBus");
        builder.Services.AddAzureServiceBusPublisher((_, o) => o.SessionIdStrategy = ServiceBusSessionStrategy.UsePartitionKeyConvertedToAnId);
        builder.Services.PostConfigureAllHealthChecks();

#if NET8_0
        // The combined telemetry patterns exceed .NET 8's default regex automata cap.
        AppContext.SetData("REGEX_NONBACKTRACKING_MAX_AUTOMATA_SIZE", 10_000);
#endif
        builder.WithCoreExTelemetry().WithCoreExCosmosDbTelemetry().WithCoreExServiceBusTelemetry().UseOtlpExporter();

        var app = builder.Build();
        app.UseCoreExExceptionHandler();
        app.UseHttpsRedirection();
        app.UseExecutionContext();
        app.MapHealthChecks();
        app.MapHostedServices(/* groupConfigure: g => g.RequireAuthorization() */);
        app.Run();
    }
}
