namespace CoreEx.IO.Csv;

/// <summary>Provides CoreEx-native file-level tracing, progress logs, and aggregate outcome metrics.</summary>
/// <param name="serviceProvider">The optional provider for logging and invoker configuration.</param>
[InvokerName("CoreEx.IO.Csv")]
public class CsvFileProcessorInvoker(IServiceProvider? serviceProvider = null) : InvokerBase<FileProcessor>(serviceProvider)
{
    /// <summary>Instruments one attempt across validation, replay, and reporting without retaining input or diagnostics.</summary>
    internal Task<FileProcessingSummary> InvokeFileAsync(FileProcessor caller, FileProcessingContext context, Func<CancellationToken, Task<FileProcessingSummary>> process, CancellationToken cancellationToken)
        => InvokeAsync(caller, async (tracer, ct) =>
        {
            var outcome = "faulted";
            tracer.Activity?.SetTag("csv.definition", context.Identity.DefinitionName);
            tracer.Activity?.SetTag("csv.definition_version", context.Identity.DefinitionVersion);
            tracer.Activity?.SetTag("csv.mode", context.Mode.ToString());
            tracer.Activity?.SetTag("csv.error_mode", context.ErrorMode.ToString());
            try
            {
                var summary = await process(ct).ConfigureAwait(false);
                outcome = summary.Outcome switch
                {
                    FileProcessingOutcome.Validated => "validated",
                    FileProcessingOutcome.Executed => "executed",
                    FileProcessingOutcome.Rejected => "rejected",
                    FileProcessingOutcome.ExecutionFailed => "execution-failed",
                    _ => throw new InvalidOperationException("Unknown file processing outcome.")
                };
                return summary;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                outcome = "cancelled";
                throw;
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "CSV processing faulted for definition {DefinitionName}.", context.Identity.DefinitionName);
                throw;
            }
            finally
            {
                var progress = context.Progress;
                tracer.Activity?.SetTag("csv.outcome", outcome);
                tracer.Activity?.SetTag("csv.records_read", progress.RecordsRead);
                tracer.Activity?.SetTag("csv.bytes_read", progress.BytesRead);
                tracer.Activity?.SetTag("csv.invalid_records", progress.InvalidRecords);
                tracer.Activity?.SetTag("csv.units_validated", progress.UnitsValidated);
                tracer.Activity?.SetTag("csv.units_attempted", progress.UnitsAttempted);
                tracer.Activity?.SetTag("csv.units_executed", progress.UnitsExecuted);
                tracer.Activity?.SetTag("csv.error_count", progress.ErrorCount);
                if (outcome is "rejected" or "execution-failed")
                    Logger?.LogWarning("CSV {Outcome}. Definition: {DefinitionName}; records: {RecordsRead}; validated: {UnitsValidated}; executed: {UnitsExecuted}; errors: {ErrorCount}.",
                        outcome, context.Identity.DefinitionName, progress.RecordsRead, progress.UnitsValidated, progress.UnitsExecuted, progress.ErrorCount);
                else
                    Logger?.LogInformation("CSV {Outcome}. Definition: {DefinitionName}; records: {RecordsRead}; validated: {UnitsValidated}; executed: {UnitsExecuted}; errors: {ErrorCount}.",
                        outcome, context.Identity.DefinitionName, progress.RecordsRead, progress.UnitsValidated, progress.UnitsExecuted, progress.ErrorCount);

                CsvProcessingTelemetry.RecordFile(outcome);
            }
        }, cancellationToken, nameof(FileProcessor.ProcessAsync));
}
