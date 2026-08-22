namespace CoreEx.Events;

/// <summary>
/// Represents the action of an event; primarily: <see cref="Created"/>, <see cref="Updated"/>, and <see cref="Deleted"/>.
/// </summary>
/// <remarks>Other common actions are also provided.</remarks>
public enum EventAction
{
    /// <summary>
    /// A <c>created</c> event action.
    /// </summary>
    Created,

    /// <summary>
    /// An <c>updated</c> event action.
    /// </summary>
    Updated,

    /// <summary>
    /// A <c>deleted</c> event action.
    /// </summary>
    Deleted,

    /// <summary>
    /// An <c>activated</c> event action.
    /// </summary>
    Activated,
    
    /// <summary>
    /// A <c>deactivated</c> event action.
    /// </summary>
    Deactivated,

    /// <summary>
    /// A <c>cancelled</c> event action.
    /// </summary>
    Cancelled,

    /// <summary>
    /// A <c>confirmed</c> event action.
    /// </summary>
    Confirmed,

    /// <summary>
    /// A <c>checked-out</c> event action.
    /// </summary>
    CheckedOut,

    /// <summary>
    /// A <c>started</c> event action.
    /// </summary>
    Started,

    /// <summary>
    /// A <c>completed</c> event action.
    /// </summary>
    Completed,

    /// <summary>
    /// A <c>paused</c> event action.
    /// </summary>
    Paused,

    /// <summary>
    /// A <c>stopped</c> event action.
    /// </summary>
    Stopped,

    /// <summary>
    /// A <c>restarted</c> event action.
    /// </summary>
    Restarted,

    /// <summary>
    /// A <c>suspended</c> event action.
    /// </summary>
    Suspended,

    /// <summary>
    /// A <c>reinstated</c> event action.
    /// </summary>
    Reinstated,

    /// <summary>
    /// A <c>closed</c> event action.
    /// </summary>
    Closed,

    /// <summary>
    /// A <c>reopened</c> event action.
    /// </summary>
    Reopened,

    /// <summary>
    /// An <c>expired</c> event action.
    /// </summary>
    Expired,

    /// <summary>
    /// A <c>renewed</c> event action.
    /// </summary>
    Renewed,

    /// <summary>
    /// A <c>submitted</c> event action.
    /// </summary>
    Submitted,

    /// <summary>
    /// An <c>approved</c> event action.
    /// </summary>
    Approved,

    /// <summary>
    /// A <c>rejected</c> event action.
    /// </summary>
    Rejected,

    /// <summary>
    /// An <c>acknowledged</c> event action.
    /// </summary>
    Acknowledged,

    /// <summary>
    /// A <c>declined</c> event action.
    /// </summary>
    Declined,

    /// <summary>
    /// A <c>sent</c> event action.
    /// </summary>
    Sent,

    /// <summary>
    /// A <c>received</c> event action.
    /// </summary>
    Received,

    /// <summary>
    /// A <c>published</c> event action.
    /// </summary>
    Published,

    /// <summary>
    /// A <c>processed</c> event action.
    /// </summary>
    Processed,

    /// <summary>
    /// A <c>failed</c> event action.
    /// </summary>
    Failed
}