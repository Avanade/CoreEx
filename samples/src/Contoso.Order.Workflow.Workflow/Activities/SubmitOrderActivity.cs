using Contoso.Order.Workflow.Workflow.Contracts;
using Microsoft.DurableTask;

namespace Contoso.Order.Workflow.Workflow.Activities;

[DurableTask]
public sealed class SubmitOrderActivity : TaskActivity<SubmitOrderActivityInput, OrderWorkflowResult>
{
    public override Task<OrderWorkflowResult> RunAsync(TaskActivityContext context, SubmitOrderActivityInput input)
    {
        var message = $"Order '{input.OrderId}' accepted for {input.Amount:0.00} {input.Currency}.";
        var result = new OrderWorkflowResult(input.OrderId, true, message, DateTimeOffset.UtcNow);
        return Task.FromResult(result);
    }
}