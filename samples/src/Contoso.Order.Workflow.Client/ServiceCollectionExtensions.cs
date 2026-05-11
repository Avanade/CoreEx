using Microsoft.DurableTask.Client;
using Microsoft.DurableTask.Client.AzureManaged;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Contoso.Order.Workflow.Client;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddContosoOrderWorkflowClient(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DurableTaskScheduler");

        var options = new DurableTaskSchedulerOptions
        {
            Endpoint = configuration[$"{DurableTaskSchedulerOptions.SectionName}:Endpoint"] ?? "http://localhost:8080",
            TaskHub = configuration[$"{DurableTaskSchedulerOptions.SectionName}:TaskHub"] ?? "order"
        };

        services.AddDurableTaskClient(durableTaskBuilder =>
        {
            durableTaskBuilder.UseDurableTaskScheduler(string.IsNullOrWhiteSpace(connectionString)
                ? DurableTaskConnectionStringFactory.Create(options)
                : connectionString);
        });

        services.AddScoped<IOrderWorkflowClient, OrderWorkflowClient>();
        return services;
    }
}