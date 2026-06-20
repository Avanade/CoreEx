namespace Contoso.E2E.Runner.Scenarios;

/// <summary>
/// Provides the attribute to define an executable test scenario.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ScenarioAttribute : RunnerAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScenarioAttribute"/> class.
    /// </summary>
    public ScenarioAttribute(string name, string text, int order = 0)
    {
        Name = name;
        Text = text;
        Order = order;
    }
}