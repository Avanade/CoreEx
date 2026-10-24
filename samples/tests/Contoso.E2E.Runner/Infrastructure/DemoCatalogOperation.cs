namespace Contoso.E2E.Runner.Infrastructure;

/// <summary>Shows durable setup stages and real work counts without a live display that could overwrite warnings.</summary>
public sealed class DemoCatalogOperation(IAnsiConsole? console = null)
{
    private readonly IAnsiConsole _console = console ?? AnsiConsole.Console;
    private readonly object _gate = new();
    private readonly Stopwatch _stageTime = new();
    private string? _stage;

    /// <summary>Runs the acknowledged bootstrap, preserving failures and cancelling on Ctrl+C.</summary>
    public async Task<int> RunAsync(IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        ConsoleCancelEventHandler cancel = (_, e) =>
        {
            e.Cancel = true;
            cancellation.Cancel();
        };
        Console.CancelKeyPress += cancel;
        var elapsed = Stopwatch.StartNew();
        try
        {
            _console.MarkupLine("[grey]Preparing demo catalog: reset Products/Shopping, synchronize products/baskets, invalidate caches, rebuild search, then verify. Ctrl+C cancels.[/]");
            var count = await DemoCatalogBootstrap.RunAsync(configuration, Report, cancellation.Token).ConfigureAwait(false);
            cancellation.Token.ThrowIfCancellationRequested();
            _console.MarkupLine($"[green]Demo bootstrap complete: {count} products replicated; baskets and lexical/semantic search verified in {elapsed.Elapsed.TotalSeconds:F1}s.[/]");
            return count;
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            _console.MarkupLine("[yellow]Demo bootstrap cancelled. Setup may be incomplete; rerun before starting hosts.[/]");
            throw;
        }
        catch (Exception ex)
        {
            _console.MarkupLine($"[red]Demo bootstrap failed during {(_stage ?? "configuration").EscapeMarkup()} after {elapsed.Elapsed.TotalSeconds:F1}s: {ex.Message.EscapeMarkup()}[/]");
            throw;
        }
        finally
        {
            Console.CancelKeyPress -= cancel;
        }
    }

    /// <summary>Synchronously renders ordered stage events; redirected output uses the same truthful, bounded lines.</summary>
    public void Report(ProductSearchIndex.RebuildProgress progress)
    {
        lock (_gate)
        {
            if (_stage != progress.Stage)
            {
                _stage = progress.Stage;
                _stageTime.Restart();
                _console.MarkupLine($"[grey]▶[/] {progress.Stage.EscapeMarkup()}...");
            }

            if (progress.IsComplete)
                _console.MarkupLine($"[green]  ✓[/] {progress.Stage.EscapeMarkup()}{Counts(progress)} [grey]({_stageTime.Elapsed.TotalSeconds:F1}s)[/]");
            else if (progress.Completed is { } completed && (completed <= 1 || completed % 10 == 0 || completed == progress.Total))
                _console.MarkupLine($"[grey]  {progress.Stage.EscapeMarkup()}{Counts(progress)}[/]");
        }
    }

    /// <summary>Formats only measured work, never estimated percentages.</summary>
    private static string Counts(ProductSearchIndex.RebuildProgress progress)
        => progress.Completed is { } completed && progress.Total is { } total ? $" {completed}/{total}" : string.Empty;
}
