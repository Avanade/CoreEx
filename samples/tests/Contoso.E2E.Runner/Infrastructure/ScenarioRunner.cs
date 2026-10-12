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

        var hasErrorLinks = false;

        foreach (var result in results)
        {
            var status = result.Success ? "[green]✓ PASS[/]" : "[red]✗ FAIL[/]";

            if (!result.Success && TryWriteErrorFile(title, result) is { } errorUri)
            {
                status = $"[red link={errorUri.EscapeMarkup()}]✗ FAIL[/]";
                hasErrorLinks = true;
            }

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

        if (hasErrorLinks)
            AnsiConsole.MarkupLine("[grey]Ctrl+click a FAIL to view the error details.[/]");

        var successCount = results.Count(r => r.Success);
        var totalCount = results.Count;
        var successRate = totalCount > 0 ? (double)successCount / totalCount * 100 : 0;

        var summaryColor = successCount == totalCount ? "green" : (successCount > 0 ? "yellow" : "red");
        var rule = new Rule($"[bold {summaryColor}]{title}: {successCount}/{totalCount} steps passed ({successRate:F0}%)[/]")
            .RuleStyle(summaryColor);

        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Writes the full error detail of a failed step to a temp file and returns its <c>file://</c> URI (or <see langword="null"/> where there is nothing to write or the write fails).
    /// </summary>
    private static string? TryWriteErrorFile(string title, StepResult result)
    {
        try
        {
            var dir = Path.Combine(Path.GetTempPath(), "Contoso.E2E.Runner", "errors");
            Directory.CreateDirectory(dir);

            var name = string.Concat(result.Name.Select(c => char.IsLetterOrDigit(c) ? c : '-')).Trim('-');
            var path = Path.Combine(dir, $"{DateTime.Now:yyyyMMdd-HHmmss-fff}-{(name.Length > 60 ? name[..60] : name)}.txt");

            var content = new StringBuilder()
                .AppendLine($"Scenario: {title}")
                .AppendLine($"Step:     {result.Name}")
                .AppendLine($"Duration: {result.Duration.TotalMilliseconds:F0}ms")
                .AppendLine()
                .AppendLine(result.Exception?.ToString() ?? result.Details);

            File.WriteAllText(path, content.ToString());
            return new Uri(path).AbsoluteUri;
        }
        catch (Exception)
        {
            return null;
        }
    }
}