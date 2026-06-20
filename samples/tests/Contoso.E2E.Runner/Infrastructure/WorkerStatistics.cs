namespace Contoso.E2E.Runner.Infrastructure;

/// <summary>
/// Thread-safe aggregator for worker statistics.
/// </summary>
public class WorkerStatistics
{
    private int _totalIterations;
    private int _successCount;
    private int _failureCount;
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    /// <summary>
    /// Gets the total number of iterations.
    /// </summary>
    public int TotalIterations => Volatile.Read(ref _totalIterations);

    /// <summary>
    /// Gets the number of successful iterations.
    /// </summary>
    public int SuccessCount => Volatile.Read(ref _successCount);

    /// <summary>
    /// Gets the number of failed iterations.
    /// </summary>
    public int FailureCount => Volatile.Read(ref _failureCount);

    /// <summary>
    /// Gets the elapsed time since the start of the simulation.
    /// </summary>
    public TimeSpan Elapsed => _stopwatch.Elapsed;

    /// <summary>
    /// Gets the success rate as a percentage.
    /// </summary>
    public double SuccessRate
    {
        get
        {
            var total = TotalIterations;
            return total > 0 ? (double)SuccessCount / total * 100 : 0;
        }
    }

    /// <summary>
    /// Gets the throughput in iterations per minute.
    /// </summary>
    public double IterationsPerMinute
    {
        get
        {
            var elapsed = Elapsed.TotalMinutes;
            return elapsed > 0 ? TotalIterations / elapsed : 0;
        }
    }

    /// <summary>
    /// Records a successful iteration.
    /// </summary>
    public void RecordSuccess()
    {
        Interlocked.Increment(ref _totalIterations);
        Interlocked.Increment(ref _successCount);
    }

    /// <summary>
    /// Records a failed iteration.
    /// </summary>
    public void RecordFailure()
    {
        Interlocked.Increment(ref _totalIterations);
        Interlocked.Increment(ref _failureCount);
    }
}