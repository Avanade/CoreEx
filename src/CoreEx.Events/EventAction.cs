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
    /// A <c>checked-out</c> event action.
    /// </summary>
    CheckedOut,

    /// <summary>
    /// A <c>completed</c> event action.
    /// </summary>
    Completed,

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