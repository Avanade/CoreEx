namespace CoreEx.Events;

/// <summary>
/// Represents the type of message.
/// </summary>
public enum MessageType
{
    /// <summary>
    /// The message is an event.
    /// </summary>
    Event = 0,

    /// <summary>
    /// The message is a command.
    /// </summary>
    Command = 1,

    /// <summary>
    /// The message is a reply-to.
    /// </summary>
    ReplyTo = 2
}