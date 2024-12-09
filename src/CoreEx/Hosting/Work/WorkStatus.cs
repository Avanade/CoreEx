// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Hosting.Work
{
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
        /// Indicates that the underlying work has been cancelled.
        /// </summary>
        Canceled = 128,

        /// <summary>
        /// Indicates that the underlying work is in progress; either started or indeterminate.
        /// </summary>
        InProgress = Started | Indeterminate,

        /// <summary>
        /// Indicates that the underlying work is executing; either created or in progress.
        /// </summary>
        Executing = Created | InProgress,

        /// <summary>
        /// Indicates the the underlying work has been terminated; either expired, failed or cancelled.
        /// </summary>
        Terminated = Expired | Failed | Canceled,

        /// <summary>
        /// Indicates that the underlying work has been completed, expired, failed or cancelled.
        /// </summary>
        Finished = Completed | Expired | Failed | Canceled
    }
}   