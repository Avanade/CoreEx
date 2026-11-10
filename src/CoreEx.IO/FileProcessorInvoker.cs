namespace CoreEx.IO;

/// <summary>Provides CoreEx-native file-level tracing, progress logs, and aggregate outcome metrics.</summary>
/// <param name="serviceProvider">The optional provider for logging and invoker configuration.</param>
[InvokerName("CoreEx.IO")]
public class FileProcessorInvoker(IServiceProvider? serviceProvider = null) : InvokerBase<FileProcessor>(serviceProvider)
{
    /// <summary>Instruments one attempt across validation, replay, and reporting without retaining input or diagnostics.</summary>
    internal Task<FileProcessingSummary> InvokeFileAsync(FileProcessor caller, FileProcessingContext context, Func<CancellationToken, Task<FileProcessingSummary>> process, CancellationToken cancellationToken)
        => InvokeAsync(caller, async (tracer, ct) =>
        {
            var outcome = "faulted";
            tracer.Activity?.SetTag("file.definition", context.Identity.DefinitionName);
            tracer.Activity?.SetTag("file.definition_version", context.Identity.DefinitionVersion);
            tracer.Activity?.SetTag("file.mode", context.Mode.ToString());
            tracer.Activity?.SetTag("file.error_mode", context.ErrorMode.ToString());
            try
            {
                var summary = await process(ct).ConfigureAwait(false);

                outcome = summary.Outcome switch
                {
                    FileProcessingOutcome.Succeeded => "succeeded",
                    FileProcessingOutcome.ContentFailed => "content-failed",
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
                Logger?.LogError(ex, "File processing faulted for definition {DefinitionName}.", context.Identity.DefinitionName);
                throw;
            }
            finally
            {
                var progress = context.Progress;
                tracer.Activity?.SetTag("file.outcome", outcome);
                tracer.Activity?.SetTag("file.records_read", progress.RecordsRead);
                tracer.Activity?.SetTag("file.bytes_read", progress.BytesRead);
                tracer.Activity?.SetTag("file.invalid_records", progress.InvalidRecords);
                tracer.Activity?.SetTag("file.units_read", progress.UnitsRead);
                tracer.Activity?.SetTag("file.invalid_units", progress.InvalidUnits);
                tracer.Activity?.SetTag("file.units_validated", progress.UnitsValidated);
                tracer.Activity?.SetTag("file.units_attempted", progress.UnitsAttempted);
                tracer.Activity?.SetTag("file.units_executed", progress.UnitsExecuted);
                tracer.Activity?.SetTag("file.error_count", progress.ErrorCount);

                if (outcome is "content-failed" or "execution-failed")
                    Logger?.LogWarning("File {Outcome}. Definition: {DefinitionName}; records: {RecordsRead}; validated: {UnitsValidated}; executed: {UnitsExecuted}; errors: {ErrorCount}.",
                        outcome, context.Identity.DefinitionName, progress.RecordsRead, progress.UnitsValidated, progress.UnitsExecuted, progress.ErrorCount);
                else if (Logger?.IsEnabled(LogLevel.Information) == true)
                    Logger?.LogInformation("File {Outcome}. Definition: {DefinitionName}; records: {RecordsRead}; validated: {UnitsValidated}; executed: {UnitsExecuted}; errors: {ErrorCount}.",
                        outcome, context.Identity.DefinitionName, progress.RecordsRead, progress.UnitsValidated, progress.UnitsExecuted, progress.ErrorCount);

                FileProcessingTelemetry.RecordFile(outcome);
            }
        }, cancellationToken, nameof(FileProcessor.ProcessAsync));
}
