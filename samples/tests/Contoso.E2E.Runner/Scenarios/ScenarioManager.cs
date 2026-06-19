namespace Contoso.E2E.Runner.Scenarios;

/// <summary>
/// Provides functionality to discover and manage scenario definitions from assemblies using scenario attributes.
/// </summary>
public sealed class ScenarioManager
{
    /// <summary>
    /// Creates a new instance of the <see cref="ScenarioManager"/> class for the calling assembly.
    /// </summary>
    public static ScenarioManager Create() => Create(Assembly.GetCallingAssembly());

    /// <summary>
    /// Creates a new instance of the <see cref="ScenarioManager"/> class for the specified assemblies.
    /// </summary>
    public static ScenarioManager Create(params IEnumerable<Assembly> assemblies) => new() { Scenarios = RunnerAttribute.GetScenarios<ScenarioAttribute>(assemblies) };

    /// <summary>
    /// Gets the collection of scenario definitions, keyed by scenario name.
    /// </summary>
    public required Dictionary<string, ScenarioDefinition> Scenarios { get; init; }
}