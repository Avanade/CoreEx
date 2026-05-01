using Contoso.Order.Workflow.Workflow.Activities;
using Contoso.Order.Workflow.Workflow.Contracts;
using Microsoft.DurableTask;

namespace Contoso.Order.Workflow.Workflow;

[DurableTask]
public sealed class OrderWorkflowOrchestration : TaskOrchestrator<OrderWorkflowRequest, OrderWorkflowResult>
{
    public override async Task<OrderWorkflowResult> RunAsync(TaskOrchestrationContext context, OrderWorkflowRequest input)
    {
        var validation = await context.CallActivityAsync<bool>(
            nameof(ValidateOrderActivity),
            new ValidateOrderActivityInput(input.OrderId, input.Amount, input.Currency));

        if (!validation)
        {
            return new OrderWorkflowResult(input.OrderId, false, "Order request failed validation.", context.CurrentUtcDateTime);
        }

        var submission = await context.CallActivityAsync<OrderWorkflowResult>(
            nameof(SubmitOrderActivity),
            new SubmitOrderActivityInput(input.OrderId, input.Amount, input.Currency, input.RequestedBy));

        return submission;
    }
}