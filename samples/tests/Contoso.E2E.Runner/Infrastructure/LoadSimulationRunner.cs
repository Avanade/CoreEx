namespace Contoso.E2E.Runner.Infrastructure;

/// <summary>
/// Manages the continuous load simulation with multiple parallel workers.
/// </summary>
public class LoadSimulationRunner
{
    private readonly TestContext _context;
    private readonly LoadSimulationConfig _config;
    private readonly RecentEventsBuffer _recentEvents;
    private readonly WorkerStatistics _statistics = new();
    private readonly ConcurrentDictionary<string, WorkerStatistics> _scenarioStatistics = [];
    private readonly string _errorLogPath;
    private readonly object _errorLogLock = new();
    private readonly DateTimeOffset _startTimeUtc = DateTimeOffset.UtcNow;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoadSimulationRunner"/> class.
    /// </summary>
    public LoadSimulationRunner(TestContext context)
    {
        _context = context;
        _config = new LoadSimulationConfig();
        _context.Config.GetSection("E2E").Bind(_config);
        _recentEvents = new RecentEventsBuffer(_config.RecentEventsDisplayCount);

        foreach (var scenario in _context.Scenarios)
            _scenarioStatistics[scenario.Key] = new WorkerStatistics();

        // Set up error log path next to the exe
        var logsDir = Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(logsDir);
        _errorLogPath = Path.Combine(logsDir, "load-simulation-errors.log");
    }

    /// <summary>
    /// Runs the load simulation until ESC is pressed.
    /// </summary>
    public async Task RunAsync()
    {
        // Clear screen and start fresh.
        AnsiConsole.Clear();

        // Initialize error log (overwrite from previous run)
        File.WriteAllText(_errorLogPath, $"Load Simulation Error Log - {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");
        File.AppendAllText(_errorLogPath, new string('=', 80) + "\n\n");

        // Enable cancellation infrastructure.
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        // Start workers
        var workers = new List<Task>();
        foreach (var scenario in _context.Scenarios)
            workers.AddRange(RunWorkersAsync(scenario.Key, scenario.Value.Factory, cancellationToken));

        // Use Spectre's built-in spinner.
        var spinner = Spinner.Known.Dots;
        var spinnerFrames = spinner.Frames.ToArray();
        var spinnerIndex = 0;

        // Start display and ESC listener.
        await AnsiConsole.Live(CreateDisplay(spinnerFrames[0]))
            .StartAsync(async ctx =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    // Check for ESC key
                    if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape)
                    {
                        cts.Cancel();
                        break;
                    }

                    // Update spinner animation.
                    spinnerIndex = (spinnerIndex + 1) % spinnerFrames.Length;

                    // Update display.
                    ctx.UpdateTarget(CreateDisplay(spinnerFrames[spinnerIndex]));
                    await Task.Delay(250);
                }

                // Wait for all workers to complete their current iteration.
                await Task.WhenAll(workers);

                // Final display update with actual final stats - checkmark instead of spinner.
                ctx.UpdateTarget(CreateDisplay("✓"));
            });

        // Let user see final results
        AnsiConsole.WriteLine();
        AnsiConsole.Markup("[grey]Press any key to continue...[/]");
        Console.ReadKey(true);
    }

    /// <summary>
    /// Spins up the configured number or workers and runs them in asynchronously parallel.
    /// </summary>
    private List<Task> RunWorkersAsync(string name, Func<IScenario> factory, CancellationToken cancellationToken)
    {
        var simulatorConfig = _config.Simulations.GetValueOrDefault(name, new LoadSimulationSimulatorConfig());
        var workers = new List<Task>();

        for (int i = 0; i < simulatorConfig.Parallelism; i++)
        {
            var workerId = i + 1;
            workers.Add(Task.Run(() => RunWorkerAsync($"{name}-{workerId}", simulatorConfig, factory, cancellationToken), cancellationToken));
        }

        return workers;
    }

    /// <summary>
    /// Create the scenario and run.
    /// </summary>
    private Task RunWorkerAsync(string workerName, LoadSimulationSimulatorConfig config, Func<IScenario> factory, CancellationToken cancellationToken)
    {
        var scenario = factory();
        return RunWorkerLoopAsync(config, workerName, scenario, cancellationToken);
    }

    /// <summary>
    /// Run the scenario in a loop until canceled.
    /// </summary>
    private async Task RunWorkerLoopAsync(LoadSimulationSimulatorConfig config, string workerName, IScenario scenario, CancellationToken cancellationToken)
    {
        var results = new List<StepResult>();
        var context = new ScenarioContext(_context, results, silentMode: true);

        // Extract scenario name from worker name (e.g., "Products-Update-1" -> "Products-Update")
        var scenarioName = workerName[..workerName.LastIndexOf('-')];

        while (!cancellationToken.IsCancellationRequested)
        {
            // Random delay between iterations.
            if (!cancellationToken.IsCancellationRequested)
            {
                var delay = Random.Shared.Next(config.MinDelayMilliseconds, config.MaxDelayMilliseconds);
                try
                {
                    await Task.Delay(delay, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected on cancellation
                }
            }

            var sw = Stopwatch.StartNew();
            results.Clear();

            try
            {
                await scenario.RunAsync(context);
                sw.Stop();

                // Update both global and scenario-specific statistics
                _statistics.RecordSuccess();
                _scenarioStatistics[scenarioName].RecordSuccess();
                _recentEvents.Add(workerName, true, "Completed", sw.Elapsed);
            }
            catch (Exception ex)
            {
                sw.Stop();

                // Update both global and scenario-specific statistics
                _statistics.RecordFailure();
                _scenarioStatistics[scenarioName].RecordFailure();

                // Log full error details to file
                LogError(workerName, ex);

                var errorMessage = ex.Message.Length > 100 ? ex.Message[..97] + "..." : ex.Message;
                _recentEvents.Add(workerName, false, $"Failed: {errorMessage}", sw.Elapsed);
            }
        }
    }

    /// <summary>
    /// Logs full error details to the error log file.
    /// </summary>
    private void LogError(string workerName, Exception ex)
    {
        lock (_errorLogLock)
        {
            var errorEntry = new StringBuilder();
            errorEntry.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Worker: {workerName}");
            errorEntry.AppendLine($"Exception Type: {ex.GetType().FullName}");
            errorEntry.AppendLine($"Message: {ex.Message}");
            if (ex.InnerException != null)
            {
                errorEntry.AppendLine($"Inner Exception: {ex.InnerException.GetType().FullName}");
                errorEntry.AppendLine($"Inner Message: {ex.InnerException.Message}");
            }
            errorEntry.AppendLine($"Stack Trace:");
            errorEntry.AppendLine(ex.StackTrace);
            errorEntry.AppendLine(new string('-', 80));
            errorEntry.AppendLine();

            File.AppendAllText(_errorLogPath, errorEntry.ToString());
        }
    }

    /// <summary>
    /// Creates the live display panel.
    /// </summary>
    private Panel CreateDisplay(string spinnerFrame)
    {
        // Create the stats table
        var totalWorkers = 0;
        var table = new Table().Border(TableBorder.Rounded).BorderColor(Color.Blue);

        // Make "Failed" column header a clickable link to error log
        var failedHeader = _statistics.FailureCount > 0
            ? $"[red][link=file:///{_errorLogPath.Replace('\\', '/')}]Failed[/][/]"
            : "Failed";

        table.AddColumns(
            new TableColumn("Scenario").LeftAligned(),
            new TableColumn("Workers").Centered(),
            new TableColumn("Iterations").RightAligned(),
            new TableColumn("Success").RightAligned(),
            new TableColumn(failedHeader).RightAligned(),
            new TableColumn("Rate").RightAligned(),
            new TableColumn("Throughput").RightAligned());

        foreach (var scenario in _context.Scenarios.Keys)
        {
            var scenarioStatistics = _scenarioStatistics[scenario];
            var workers = _config.Simulations[scenario].Parallelism;
            totalWorkers += workers;

            table.AddRow(
                scenario.EscapeMarkup(),
                workers.ToString(),
                scenarioStatistics.TotalIterations.ToString(),
                $"[green]{scenarioStatistics.SuccessCount}[/]",
                $"[red]{scenarioStatistics.FailureCount}[/]",
                $"{scenarioStatistics.SuccessRate:F1}%",
                $"{scenarioStatistics.IterationsPerMinute:F0}/min");
        }

        // Add total row
        table.AddRow(
            "[bold]TOTAL[/]",
            $"[bold]{totalWorkers}[/]",
            $"[bold]{_statistics.TotalIterations}[/]",
            $"[green bold]{_statistics.SuccessCount}[/]",
            $"[red bold]{_statistics.FailureCount}[/]",
            $"[bold]{_statistics.SuccessRate:F1}%[/]",
            $"[bold]{_statistics.IterationsPerMinute:F0}/min[/]");

        // Build recent events markup
        var eventsContent = new StringBuilder();
        if (_config.RecentEventsDisplayCount > 0)
        {
            var recentEvents = _recentEvents.GetRecent();

            if (recentEvents.Count > 0)
            {
                eventsContent.AppendLine();
                eventsContent.AppendLine($"[bold]Recent Events (last {Math.Min(recentEvents.Count, _config.RecentEventsDisplayCount)}):[/]");
                foreach (var evt in recentEvents.Take(_config.RecentEventsDisplayCount))
                {
                    var icon = evt.Success ? "[green]✓[/]" : "[red]✗[/]";
                    var time = evt.Timestamp.ToString("HH:mm:ss");
                    var workerName = evt.WorkerName.EscapeMarkup();
                    var message = evt.Message.EscapeMarkup();
                    eventsContent.AppendLine($"  {time} [grey]{workerName}[/] {icon} {message} [grey]({evt.Duration.TotalMilliseconds:F0}ms)[/]");
                }
            }
            else
            {
                eventsContent.AppendLine();
                eventsContent.AppendLine("[grey]Waiting for events...[/]");
            }
        }

        // Combine table and events using Rows
        var combinedContent = new Rows(
            table,
            new Markup(eventsContent.ToString())
        );

        var duration = DateTimeOffset.UtcNow - _startTimeUtc;

        // Create single panel with everything
        return new Panel(combinedContent)
        {
            Header = new PanelHeader($"[bold blue] {spinnerFrame}[/] [bold]Load Simulation Running[/] {duration:c} [grey](Press ESC to stop)[/]"),
            Border = BoxBorder.Double,
            BorderStyle = new Style(Color.Blue),
            Expand = false
        };
    }
}