namespace CoreEx.IO;

/// <summary>Runs a processor with an optional, awaited invocation report.</summary>
public static class FileProcessingReporter
{
    /// <summary>Starts the report, streams diagnostics, and finalizes an outcome or operational failure.</summary>
    /// <typeparam name="TUnit">The processor's unit type.</typeparam>
    /// <param name="processor">The file processor.</param>
    /// <param name="openReadAsync">The processor's immutable fresh-stream factory.</param>
    /// <param name="context">A fresh invocation-local context.</param>
    /// <param name="report">The invocation-local report.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The unchanged processor summary; callers decide whether to throw a returned failure.</returns>
    /// <remarks>Existing processor hooks still run before report diagnostics. An <see cref="OperationCanceledException"/>, a canceled invocation token, or a diagnostic-report failure prevents fault finalization. A failure during finalization preserves any earlier processing or returned execution failure in an aggregate, including cancellation during finalization. Reports are not transactional with business effects.</remarks>
    public static async Task<FileProcessingSummary> RunAsync<TUnit>(FileProcessor<TUnit> processor, Func<CancellationToken, Task<Stream>> openReadAsync, FileProcessingContext context, IFileProcessingReport report, CancellationToken cancellationToken = default) where TUnit : class
    {
        processor.ThrowIfNull();
        openReadAsync.ThrowIfNull();
        context.ThrowIfNull();
        report.ThrowIfNull();
        var reportFaulted = false;

        async Task ObserveAsync(FileDiagnosticArgs args, CancellationToken ct)
        {
            try
            {
                await report.WriteDiagnosticAsync(args, ct).ConfigureAwait(false);
            }
            catch
            {
                reportFaulted = true;
                throw;
            }
        }

        Func<FileDiagnosticArgs, CancellationToken, Task> observer = ObserveAsync;
        if (Interlocked.CompareExchange(ref context.DiagnosticObserver, observer, null) is not null)
            throw new InvalidOperationException("The file context already has an active report.");

        try
        {
            await report.StartAsync(context, cancellationToken).ConfigureAwait(false);
            FileProcessingSummary summary;
            try
            {
                summary = await processor.ProcessAsync(openReadAsync, context, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception failure) when (!reportFaulted && !cancellationToken.IsCancellationRequested && failure is not OperationCanceledException)
            {
                try
                {
                    await report.CompleteAsync(new(context, null, failure), cancellationToken).ConfigureAwait(false);
                }
                catch (Exception reportFailure)
                {
                    throw new AggregateException("File processing and report finalization both failed.", failure, reportFailure);
                }

                throw;
            }

            var executionFailure = summary.ExecutionResult.IsFailure ? summary.ExecutionResult.Error : null;
            try
            {
                await report.CompleteAsync(new(context, summary, executionFailure), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception reportFailure) when (executionFailure is not null)
            {
                throw new AggregateException("File execution and report finalization both failed.", executionFailure, reportFailure);
            }

            return summary;
        }
        finally
        {
            Interlocked.CompareExchange(ref context.DiagnosticObserver, null, observer);
        }
    }
}
