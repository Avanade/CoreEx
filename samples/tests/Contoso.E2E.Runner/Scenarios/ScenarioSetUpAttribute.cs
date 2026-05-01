namespace Contoso.E2E.Runner.Scenarios;

/// <summary>
/// Provides the attribute to define an executable off-off set-up scenario.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ScenarioSetUpAttribute : RunnerAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScenarioSetUpAttribute"/> class.
    /// </summary>
    public ScenarioSetUpAttribute(string name, string text, int order = 0, bool requiresApi = true)
    {
        Name = name;
        Text = text;
        Order = order;
        RequiresApi = requiresApi;
    }
}