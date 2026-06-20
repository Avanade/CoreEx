namespace Contoso.E2E.Runner.Infrastructure;

/// <summary>
/// Represents the result of a scenario step.
/// </summary>
public record StepResult(string Name, bool Success, TimeSpan Duration, string Details, Exception? Exception = null);

/// <summary>
/// Provides scenario execution with progress tracking and visual reporting.
/// </summary>
public class ScenarioRunner(TestContext context)
{
    private readonly TestContext _context = context;

    /// <summary>
    /// Runs a scenario with progress tracking.
    /// </summary>
    public async Task<List<StepResult>> RunScenarioAsync(ScenarioDefinition scenarioDefinition)
    {
        var results = new List<StepResult>();
        var context = new ScenarioContext(_context, results, silentMode: false);

        var scenario = scenarioDefinition.Factory();

        AnsiConsole.Write(new Rule($"[bold blue]{scenarioDefinition.Text}[/]").RuleStyle("blue").LeftJustified());
        AnsiConsole.WriteLine();

        try
        {
            await scenario.RunAsync(context);
        }
        catch (Exception) { }

        AnsiConsole.WriteLine();
        DisplayResults(scenarioDefinition.Text, results);

        return results;
    }

    private static void DisplayResults(string title, List<StepResult> results)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn("[bold]Step[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]Status[/]").Centered())
            .AddColumn(new TableColumn("[bold]Duration[/]").RightAligned())
            .AddColumn(new TableColumn("[bold]Details[/]").LeftAligned());

        foreach (var result in results)
        {
            var status = result.Success ? "[green]✓ PASS[/]" : "[red]✗ FAIL[/]";
            var duration = $"{result.Duration.TotalMilliseconds:F0}ms";
            var details = result.Details.Length > 200 ? result.Details[..197] + "..." : result.Details;

            table.AddRow(
                result.Name.EscapeMarkup(),
                status,
                duration,
                details.EscapeMarkup()
            );
        }

        AnsiConsole.Write(table);

        var successCount = results.Count(r => r.Success);
        var totalCount = results.Count;
        var successRate = totalCount > 0 ? (double)successCount / totalCount * 100 : 0;

        var summaryColor = successCount == totalCount ? "green" : (successCount > 0 ? "yellow" : "red");
        var rule = new Rule($"[bold {summaryColor}]{title}: {successCount}/{totalCount} steps passed ({successRate:F0}%)[/]")
            .RuleStyle(summaryColor);

        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();
    }
}