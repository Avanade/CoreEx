namespace Contoso.E2E.Runner.Infrastructure;

/// <summary>
/// Provides the context passed to scenario execution for step tracking.
/// </summary>
public class ScenarioContext(TestContext context, List<StepResult> results, bool silentMode = false)
{
    private readonly TestContext _context = context;
    private readonly List<StepResult> _results = results;
    private readonly bool _silentMode = silentMode;

    public TestContext TestContext => _context;

    /// <summary>
    /// Executes a step with timing, error handling, and reporting with a result.
    /// </summary>
    public async Task<T> StepAsync<T>(string stepName, Func<Task<T>> action, Func<T, string>? detailsFormatter = null)
    {
        if (!_silentMode)
            AnsiConsole.MarkupLine($"[grey]▶[/] {stepName.EscapeMarkup()}...");

        var sw = Stopwatch.StartNew();

        try
        {
            var result = await action();
            sw.Stop();

            var details = detailsFormatter?.Invoke(result) ?? result?.ToString() ?? "Success";
            _results.Add(new StepResult(stepName, true, sw.Elapsed, details));

            if (!_silentMode)
                AnsiConsole.MarkupLine($"[green]  ✓[/] {stepName.EscapeMarkup()} [grey]({sw.ElapsedMilliseconds}ms)[/]");

            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _results.Add(new StepResult(stepName, false, sw.Elapsed, ex.Message, ex));

            if (!_silentMode)
                AnsiConsole.MarkupLine($"[red]  ✗[/] {stepName.EscapeMarkup()} [grey]({sw.ElapsedMilliseconds}ms)[/]");

            throw;
        }
    }

    /// <summary>
    /// Executes a step with timing, error handling, and reporting with no result.
    /// </summary>
    public async Task StepAsync(string stepName, Func<Task> action, string? successDetails = null)
    {
        await StepAsync(stepName, async () =>
        {
            await action();
            return successDetails ?? "Completed.";
        }, details => details);
    }

    /// <summary>
    /// Adds a randomized delay to simulate a pseudo user (who is pretty bloody speedy) pause;versus, overwhelming bang-bang.
    /// </summary>
    public static Task RandomizedDelayAsync(ScenarioContext ctx, CancellationToken cancellationToken = default)
        => Task.Delay(Random.Shared.Next(ctx.TestContext.PerStepMinDelayMilliseconds, ctx.TestContext.PerStepMaxDelayMilliseconds), cancellationToken);
}