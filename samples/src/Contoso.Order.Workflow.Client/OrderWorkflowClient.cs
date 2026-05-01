using Contoso.Order.Workflow.Workflow;
using Contoso.Order.Workflow.Workflow.Contracts;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace Contoso.Order.Workflow.Client;

public sealed class OrderWorkflowClient
{
    private readonly DurableTaskClient _durableTaskClient;
    private readonly ILogger<OrderWorkflowClient> _logger;

    public OrderWorkflowClient(DurableTaskClient durableTaskClient, ILogger<OrderWorkflowClient> logger)
    {
        _durableTaskClient = durableTaskClient;
        _logger = logger;
    }

    public async Task<string> StartAsync(OrderWorkflowRequest request, string? instanceId = null, CancellationToken cancellationToken = default)
    {
        var startOptions = string.IsNullOrWhiteSpace(instanceId)
            ? null
            : new StartOrchestrationOptions(instanceId);

        var orchestrationInstanceId = await _durableTaskClient.ScheduleNewOrchestrationInstanceAsync(
            nameof(OrderWorkflowOrchestration),
            request,
            startOptions,
            cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Scheduled {OrchestrationName} with instance id {InstanceId}.", nameof(OrderWorkflowOrchestration), orchestrationInstanceId);
        return orchestrationInstanceId;
    }

    public Task<OrchestrationMetadata?> GetMetadataAsync(string instanceId, bool getInputsAndOutputs = false, CancellationToken cancellationToken = default)
        => _durableTaskClient.GetInstanceAsync(instanceId, getInputsAndOutputs, cancellationToken);
}