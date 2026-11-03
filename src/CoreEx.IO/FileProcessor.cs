namespace CoreEx.IO;

/// <summary>Defines a format-neutral, file-invocation coordinator and its reporting extension points.</summary>
/// <remarks>This contract does not implement parsing or scope orchestration. Derived processors must await callbacks, preserve parent execution contexts, and resolve unit handlers inside their own scopes.</remarks>
public abstract class FileProcessor
{
    /// <summary>Processes immutable input, optionally reopening it only after whole-file acceptance.</summary>
    /// <param name="openReadAsync">Opens a fresh readable stream at the beginning of identical bytes on each call. The processor disposes each returned stream. Revision labels alone do not enforce immutability.</param>
    /// <param name="context">The invocation-local context; do not reuse it concurrently or across attempts.</param>
    /// <param name="cancellationToken">The processing cancellation token, forwarded to opening, reading, and callbacks.</param>
    /// <returns>The processing summary, including rejection or returned business execution failure. Operational faults and cancellation propagate.</returns>
    public abstract Task<FileProcessingSummary> ProcessAsync(Func<CancellationToken, Task<Stream>> openReadAsync, FileProcessingContext context, CancellationToken cancellationToken = default);

    /// <summary>Validates file-wide reconciliation at EOF; emit errors without writes or retained diagnostic history.</summary>
    /// <param name="context">The validation-pass context containing reconciliation state.</param>
    /// <param name="reportDiagnosticAsync">The awaited diagnostic sink enforcing counting, continuation policy, and file-level context.</param>
    /// <param name="cancellationToken">The processing cancellation token.</param>
    /// <returns>The asynchronous validation operation.</returns>
    protected virtual Task OnValidateFileAsync(FileProcessingContext context, Func<FileDiagnostic, CancellationToken, Task> reportDiagnosticAsync, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>Observes one diagnostic in input order under the file-level context; failures propagate.</summary>
    /// <param name="context">The file invocation context, not a unit's scoped execution context.</param>
    /// <param name="diagnostic">The structured diagnostic without raw input values by default.</param>
    /// <param name="cancellationToken">The processing cancellation token.</param>
    /// <returns>The asynchronous reporting operation, awaited for backpressure.</returns>
    /// <remarks>The default performs no reporting; summary counts must still record diagnostics.</remarks>
    protected virtual Task OnDiagnosticAsync(FileProcessingContext context, FileDiagnostic diagnostic, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>Finalizes content rejection, including incomplete validation, under the file-level context.</summary>
    /// <param name="context">The file invocation context.</param>
    /// <param name="summary">The rejected summary with zero business execution.</param>
    /// <param name="cancellationToken">The processing cancellation token.</param>
    /// <returns>The asynchronous finalization operation; failures propagate.</returns>
    /// <remarks>Never invoked for execution failure, operational faults, or cancellation.</remarks>
    protected virtual Task OnRejectedAsync(FileProcessingContext context, FileProcessingSummary summary, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>Finalizes successful completion of the requested processing mode under the file-level context.</summary>
    /// <param name="context">The file invocation context.</param>
    /// <param name="summary">The successfully validated or executed summary.</param>
    /// <param name="cancellationToken">The processing cancellation token.</param>
    /// <returns>The asynchronous finalization operation; failures propagate without invoking another terminal hook.</returns>
    protected virtual Task OnCompletedAsync(FileProcessingContext context, FileProcessingSummary summary, CancellationToken cancellationToken) => Task.CompletedTask;
}
