namespace CoreEx.Entities;

/// <summary>
/// Represents a <see cref="MessageItem"/> collection.
/// </summary>
public class MessageItemCollection : ObservableCollection<MessageItem>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageItemCollection" /> class.
    /// </summary>
    public MessageItemCollection() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageItemCollection" /> class.
    /// </summary>
    /// <param name="messages">Initial messages to add.</param>
    public MessageItemCollection(IEnumerable<MessageItem> messages) : base(messages) { }

    /// <summary>
    /// Adds zero or more <paramref name="messages"/> to the collection.
    /// </summary>
    /// <param name="messages">The messages.</param>
    public void AddRange(IEnumerable<MessageItem> messages)
    {
        foreach (var m in messages)
            Add(m);
    }

    /// <summary>
    /// Determines whether a <see cref="MessageItem"/> exists for a selected <see cref="MessageType"/>.
    /// </summary>
    /// <param name="type">The <see cref="MessageType"/>.</param>
    /// <returns><see langword="true"/> if a message exists; otherwise, <see langword="false"/>.</returns>
    public bool ContainsType(MessageType type) => this.Any(x => x.Type == type);

    /// <summary>
    /// Gets a new <see cref="MessageItemCollection"/> items for the selected <see cref="MessageType"/>.
    /// </summary>
    /// <param name="type">The <see cref="MessageType"/>.</param>
    /// <returns>The new <see cref="MessageItemCollection"/>.</returns>
    public MessageItemCollection GetMessagesForType(MessageType type) => new(this.Where(x => x.Type == type));
}