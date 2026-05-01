namespace Contoso.E2E.Runner.Scenarios;

/// <summary>
/// Enables the <see cref="RunAsync(ScenarioContext)"/> execution.
/// </summary>
public interface IScenario
{
    /// <summary>
    /// Executes the scenario asynchronously using the specified context.
    /// </summary>
    public Task RunAsync(ScenarioContext context);
}