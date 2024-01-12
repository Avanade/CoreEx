using CoreEx.Hosting;
using CoreEx.TestFunctionIso;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureHostStartup<Startup>()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services => 
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
    })
    .Build();

host.Run();
