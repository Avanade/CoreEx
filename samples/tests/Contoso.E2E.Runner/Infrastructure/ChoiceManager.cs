namespace Contoso.E2E.Runner.Infrastructure;

/// <summary>
/// Provides a menu of choices for the user to select which scenario or set-up to run, and manages the execution of those choices.
/// </summary>
/// <param name="context">The <see cref="TestContext"/>.</param>
public class ChoiceManager(TestContext context)
{
    private const string _requiresApi = " [grey](requires APIs)[/]";
    private readonly TestContext _context = context;
    private readonly Dictionary<string, (ScenarioDefinition? Definition, int Group, Func<Task> RunAsync, ChoiceResult Result)> _choicesCache = [];

    /// <summary>
    /// Caches choices so they can be displayed and executed efficiently.
    /// </summary>
    public void CacheChoices()
    {
        Task<List<StepResult>> RunScenarioAsync(ScenarioDefinition scenarioDefinition) => new ScenarioRunner(_context).RunScenarioAsync(scenarioDefinition);

        int i = 1;
        foreach (var setUp in _context.SetUps)
            _choicesCache.Add($"{i++}. {setUp.Value.Text}{(setUp.Value.Attribute.RequiresApi ? _requiresApi : string.Empty)}", (setUp.Value, 0, () => RunScenarioAsync(setUp.Value), ChoiceResult.ContinueWithPrompt));

        foreach (var scenario in _context.Scenarios)
            _choicesCache.Add($"{i++}. {scenario.Value.Text}{(scenario.Value.Attribute.RequiresApi ? _requiresApi : string.Empty)}", (scenario.Value, 1, () => RunScenarioAsync(scenario.Value), ChoiceResult.ContinueWithPrompt));

        _choicesCache.Add($"{i++}. Run all scenarios as simulation{_requiresApi}", (null, 1, () => new LoadSimulationRunner(_context).RunAsync(), ChoiceResult.Continue));
        _choicesCache.Add($"{i++}. Retry APIs", (null, -2, () => Task.CompletedTask, ChoiceResult.RetryApi));
        _choicesCache.Add($"{i++}. Exit", (null, -1, () => Task.CompletedTask, ChoiceResult.Stop));
    }

    /// <summary>
    /// Adds the cached choices to the provided <see cref="SelectionPrompt{T}"/>, grouped by set-ups, scenarios, and other options.
    /// </summary>
    public SelectionPrompt<string> AddRunnerChoices(SelectionPrompt<string> prompt)
    {
        if (_choicesCache.Count == 0)
            CacheChoices();

        prompt.AddChoiceGroup("Set-up:", _choicesCache.Where(c => c.Value.Group == 0).Select(c => c.Key));
        prompt.AddChoiceGroup("Scenarios:", _choicesCache.Where(c => c.Value.Group == 1).Select(c => c.Key));
        prompt.AddChoiceGroup("Other:", _choicesCache.Where(c => c.Value.Group < 0).Select(c => c.Key));
        return prompt;
    }

    /// <summary>
    /// Executes the specified choice asynchronously and returns the result of the operation.
    /// </summary>
    public async Task<ChoiceResult> RunChoiceAsync(string choice, bool apisHealthy)
    {
        if (!_choicesCache.TryGetValue(choice, out var item))
            throw new InvalidOperationException($"Invalid choice: {choice}");

        if (item.Definition?.Attribute.RequiresApi == true && !apisHealthy)
            return ChoiceResult.RequiresApi;

        await item.RunAsync();
        return item.Result;
    }
}