namespace Contoso.E2E.Runner.Scenarios;

/// <summary>
/// Provides functionality to discover and manage one-off set-up scenario definitions from assemblies using scenario attributes.
/// </summary>
public sealed class SetUpManager
{
    /// <summary>
    /// Creates a new instance of the <see cref="SetUpManager"/> class for the calling assembly.
    /// </summary>
    public static SetUpManager Create() => Create(Assembly.GetCallingAssembly());

    /// <summary>
    /// Creates a new instance of the <see cref="SetUpManager"/> class for the specified assemblies.
    /// </summary>
    public static SetUpManager Create(params IEnumerable<Assembly> assemblies) => new() { SetUps = RunnerAttribute.GetScenarios<ScenarioSetUpAttribute>(assemblies) };

    /// <summary>
    /// Gets the collection of set-up scenario definitions, keyed by scenario name.
    /// </summary>
    public required Dictionary<string, ScenarioDefinition> SetUps { get; init; }
}