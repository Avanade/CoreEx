using Contoso.Order.Workflow.Workflow;
using Contoso.Order.Workflow.Workflow.Activities;
using Microsoft.DurableTask.Worker;
using Microsoft.DurableTask.Worker.AzureManaged;
using OpenTelemetry.Trace;

namespace Contoso.Order.Workflow.Worker;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(LogLevel.Information);

        var endpoint = Environment.GetEnvironmentVariable("dts-endpoint")
            ?? builder.Configuration["DurableTaskScheduler:Endpoint"]
            ?? "http://localhost:8080";

        var taskHubName = Environment.GetEnvironmentVariable("TASKHUB")
            ?? builder.Configuration["DurableTaskScheduler:TaskHub"]
            ?? "order";

        var hostAddress = endpoint.Contains(';', StringComparison.Ordinal) ? endpoint.Split(';', StringSplitOptions.TrimEntries)[0] : endpoint;
        var isLocalEmulator = hostAddress.StartsWith("http://localhost:8080", StringComparison.OrdinalIgnoreCase)
            || hostAddress.StartsWith("http://localhost:8081", StringComparison.OrdinalIgnoreCase);

        var connectionString = isLocalEmulator
            ? $"Endpoint={hostAddress};TaskHub={taskHubName};Authentication=None"
            : $"Endpoint={hostAddress};TaskHub={taskHubName};Authentication=DefaultAzure";

        builder.Services.AddDurableTaskWorker()
            .AddTasks(registry =>
            {
                registry.AddOrchestrator<OrderWorkflowOrchestration>();
                registry.AddActivity<ValidateOrderActivity>();
                registry.AddActivity<SubmitOrderActivity>();
            })
            .UseDurableTaskScheduler(connectionString);

        builder.Services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                // The DurableTask SDK registers its own ActivitySource internally for orchestration and activity tracing.
                tracing.AddOtlpExporter();
            });

        builder.Services.AddHealthChecks();

        var app = builder.Build();

        app.MapHealthChecks("/health");

        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation(
            "Order workflow worker started with endpoint: {Endpoint}, task hub: {TaskHub}, local emulator: {IsLocalEmulator}.",
            hostAddress,
            taskHubName,
            isLocalEmulator);

        app.Run();
    }
}