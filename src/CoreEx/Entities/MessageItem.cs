namespace CoreEx.Entities;

/// <summary>
/// Represents a <see cref="MessageItem"/>.
/// </summary>
[DebuggerDisplay("Type = {Type}, Text = {Text}, Property = {Property}")]
public partial record class MessageItem()
{
    /// <summary>
    /// Creates a new instance of the <see cref="MessageItem"/> class.
    /// </summary>
    /// <param name="type">The <see cref="MessageType"/>.</param>
    /// <param name="text">The message <see cref="LText"/>.</param>
    /// <param name="property">The optional property that the message relates to.</param>
    public MessageItem(MessageType type, LText text, string? property = null) : this()
    {
        Type = type;
        Text = text;
        Property = property;
    }

    /// <summary>
    /// Gets the message severity type.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public MessageType Type { get; set; }

    /// <summary>
    /// Gets or sets the message <see cref="LText"/>.
    /// </summary>
    public LText? Text { get; set; }

    /// <summary>
    /// Gets or sets the name of the property that the message relates to.
    /// </summary>
    public string? Property { get; set; }

    /// <summary>
    /// Sets the <see cref="Property"/>.
    /// </summary>
    /// <param name="property">The name of the property that the message relates to.</param>
    /// <returns>This <see cref="MessageItem"/> to support fluent-style method-chaining.</returns>
    public MessageItem WithProperty(string property)
    {
        Property = property;
        return this;
    }
}