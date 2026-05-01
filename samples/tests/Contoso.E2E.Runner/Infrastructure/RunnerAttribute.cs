namespace Contoso.E2E.Runner.Infrastructure;

/// <summary>
/// Provides the base attribute for defining test scenarios.
/// </summary>
public abstract class RunnerAttribute : Attribute
{
    /// <summary>
    /// Gets the name.
    /// </summary>
    public string Name { get; protected init; } = string.Empty;

    /// <summary>
    /// Gets the friendly text.
    /// </summary>
    public string Text { get; protected init; } = string.Empty;

    /// <summary>
    /// Gets the preferred order to display.
    /// </summary>
    public int Order { get; protected init; } = 0;

    /// <summary>
    /// Indicates whether the scenario requires API access.
    /// </summary>
    public bool RequiresApi { get; protected init; } = true;

    /// <summary>
    /// Dynamically discovers and retrieves scenario definitions from the specified assemblies based on the presence of a specific <see cref="RunnerAttribute"/>-derived attribute.
    /// </summary>
    public static Dictionary<string, ScenarioDefinition> GetScenarios<TAttribute>(params IEnumerable<Assembly>[] assemblies) where TAttribute : RunnerAttribute
        => (from assembly in assemblies.Distinct().SelectMany(a => a)
            from type in assembly.GetTypes()
            where !type.IsAbstract && !type.IsGenericTypeDefinition && typeof(IScenario).IsAssignableFrom(type) && type.GetConstructor(Type.EmptyTypes) != null
            let attr = type.GetCustomAttributes(typeof(TAttribute), true).SingleOrDefault() as TAttribute
            where attr is not null
            select new ScenarioDefinition { Attribute = attr, Factory = () => (IScenario)Activator.CreateInstance(type)! }).ToDictionary(sd => sd.Attribute.Name);
}