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
    /// <param name="args">The validation-pass context and awaited diagnostic sink.</param>
    /// <param name="cancellationToken">The processing cancellation token.</param>
    /// <returns>The asynchronous validation operation.</returns>
    protected virtual Task OnValidateFileAsync(FileValidationArgs args, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>Observes one diagnostic in input order under the file-level context; failures propagate.</summary>
    /// <param name="args">The file invocation context and emitted diagnostic.</param>
    /// <param name="cancellationToken">The processing cancellation token.</param>
    /// <returns>The asynchronous reporting operation, awaited for backpressure.</returns>
    /// <remarks>The default performs no reporting; summary counts must still record diagnostics.</remarks>
    protected virtual Task OnDiagnosticAsync(FileDiagnosticArgs args, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>Finalizes content failure, including incomplete validation or partial single-pass execution.</summary>
    /// <param name="args">The file invocation context and unsuccessful content summary.</param>
    /// <param name="cancellationToken">The processing cancellation token.</param>
    /// <returns>The asynchronous finalization operation; failures propagate.</returns>
    /// <remarks>Never invoked for business execution failure, operational faults, or cancellation.</remarks>
    protected virtual Task OnContentFailedAsync(FileSummaryArgs args, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>Observes the first business execution failure before the unsuccessful summary is returned.</summary>
    /// <param name="args">The file invocation context, failed unit provenance, and original business result.</param>
    /// <param name="cancellationToken">The processing cancellation token.</param>
    /// <returns>The asynchronous failure-reporting operation; failures propagate.</returns>
    protected virtual Task OnExecutionFailedAsync(FileExecutionFailedArgs args, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>Finalizes successful validation, reporting, or execution under the file-level context.</summary>
    /// <param name="args">The file invocation context and successful summary.</param>
    /// <param name="cancellationToken">The processing cancellation token.</param>
    /// <returns>The asynchronous finalization operation; failures propagate without invoking another terminal hook.</returns>
    protected virtual Task OnCompletedAsync(FileSummaryArgs args, CancellationToken cancellationToken) => Task.CompletedTask;
}
