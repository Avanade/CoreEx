namespace Contoso.Order.Workflow.Client;

public sealed class DurableTaskSchedulerOptions
{
    public const string SectionName = "DurableTaskScheduler";

    public string Endpoint { get; set; } = "http://localhost:8080";

    public string TaskHub { get; set; } = "order";
}