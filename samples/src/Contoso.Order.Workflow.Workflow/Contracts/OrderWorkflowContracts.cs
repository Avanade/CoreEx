namespace Contoso.Order.Workflow.Workflow.Contracts;

public record OrderWorkflowRequest(string OrderId, decimal Amount, string Currency, string? RequestedBy = null);

public record OrderWorkflowResult(string OrderId, bool Accepted, string Message, DateTimeOffset ProcessedAt);

public record ValidateOrderActivityInput(string OrderId, decimal Amount, string Currency);

public record SubmitOrderActivityInput(string OrderId, decimal Amount, string Currency, string? RequestedBy);