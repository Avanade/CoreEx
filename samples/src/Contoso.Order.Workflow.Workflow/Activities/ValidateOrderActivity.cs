using Contoso.Order.Workflow.Workflow.Contracts;
using Microsoft.DurableTask;

namespace Contoso.Order.Workflow.Workflow.Activities;

[DurableTask]
public sealed class ValidateOrderActivity : TaskActivity<ValidateOrderActivityInput, bool>
{
    public override Task<bool> RunAsync(TaskActivityContext context, ValidateOrderActivityInput input)
    {
        var hasOrderId = !string.IsNullOrWhiteSpace(input.OrderId);
        var hasCurrency = !string.IsNullOrWhiteSpace(input.Currency);
        var hasPositiveAmount = input.Amount > 0;

        return Task.FromResult(hasOrderId && hasCurrency && hasPositiveAmount);
    }
}