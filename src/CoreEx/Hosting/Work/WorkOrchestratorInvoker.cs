namespace CoreEx.Hosting.Work;

/// <summary>
/// Provides the <see cref="WorkOrchestrator"/> invoker.
/// </summary>
[InvokerName("CoreEx.Hosting.Work.WorkOrchestrator")]
public class WorkOrchestratorInvoker : InvokerBase<WorkOrchestrator>
{
    private static WorkOrchestratorInvoker? _default;

    /// <summary>
    /// Gets the default <see cref="WorkOrchestratorInvoker"/> instance.
    /// </summary>
    public static WorkOrchestratorInvoker Default => ExecutionContext.GetService<WorkOrchestratorInvoker>() ?? (_default ??= new WorkOrchestratorInvoker());
}