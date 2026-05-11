using Contoso.Order.Workflow.Workflow.Contracts;
using Microsoft.DurableTask.Client;

namespace Contoso.Order.Workflow.Client;

public interface IOrderWorkflowClient
{
    Task<string> StartAsync(OrderWorkflowRequest request, string? instanceId = null, CancellationToken cancellationToken = default);

    Task<OrchestrationMetadata?> GetMetadataAsync(string instanceId, bool getInputsAndOutputs = false, CancellationToken cancellationToken = default);
}