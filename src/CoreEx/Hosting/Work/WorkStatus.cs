namespace CoreEx.Hosting.Work;

/// <summary>
/// Represents the long-running work status.
/// </summary>
public enum WorkStatus
{
    /// <summary>
    /// Indicates that the work as been created; however, it is not yet <see cref="Started"/>.
    /// </summary>
    Created = 1,

    /// <summary>
    /// Indicates that the underlying work has been started and is in progress.
    /// </summary>
    Started = 2,

    /// <summary>
    /// Indicates that the underlying work is in progress; however, the progress is indeterminate, see associated <see cref="WorkState.Reason"/> for details.
    /// </summary>
    /// <remarks>This may occur as a result of a possible retry of processing and as such may or may not be completed; will eventually automatically expire without explicit completion.</remarks>
    Indeterminate = 4,

    /// <summary>
    /// Indicates that the underlying work has been completed successfully.
    /// </summary>
    Completed = 8,

    /// <summary>
    /// Indicates that the underlying work has failed.
    /// </summary>
    Failed = 32,

    /// <summary>
    /// Indicates that the underlying work has expired.
    /// </summary>
    Expired = 64,

    /// <summary>
    /// Indicates that the underlying work has been canceled.
    /// </summary>
    Canceled = 128,

    /// <summary>
    /// Indicates that the underlying work is in progress; either <see cref="Started"/> or <see cref="Indeterminate"/>.
    /// </summary>
    InProgress = Started | Indeterminate,

    /// <summary>
    /// Indicates that the underlying work is executing; either <see cref="Created"/> or <see cref="InProgress"/>.
    /// </summary>
    Executing = Created | InProgress,

    /// <summary>
    /// Indicates the the underlying work has been terminated; either <see cref="Expired"/>, <see cref="Failed"/> or <see cref="Canceled"/>.
    /// </summary>
    Terminated = Expired | Failed | Canceled,

    /// <summary>
    /// Indicates that the underlying work has been <see cref="Completed"/>, <see cref="Expired"/>, <see cref="Failed"/> or <see cref="Canceled"/>.
    /// </summary>
    Finished = Completed | Expired | Failed | Canceled
}