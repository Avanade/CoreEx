//using Azure.Monitor.OpenTelemetry.AspNetCore;
using CoreEx.Hosting;
using CoreEx.TestFunctionIso;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Trace;
using Azure.Monitor.OpenTelemetry.Exporter;

new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddOpenTelemetry().WithTracing(b => b.AddSource("CoreEx.*").AddAzureMonitorTraceExporter());
        //services.AddOpenTelemetry().UseAzureMonitor();
        //services.ConfigureOpenTelemetryTracerProvider(tpb => tpb.AddSource("CoreEx.*"));
    })
    .ConfigureHostStartup<Startup>()
    .Build()
    .Run();