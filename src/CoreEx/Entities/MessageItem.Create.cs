namespace CoreEx.Entities;

public partial record class MessageItem
{
    /// <summary>
    /// Creates an error message using a property's JSON path and optional message text.
    /// </summary>
    /// <typeparam name="TEntity">The root entity type.</typeparam>
    /// <param name="propertyExpression">The direct, nested, or indexed property expression.</param>
    /// <param name="text">The message; null uses the standard invalid format and friendly metadata label.</param>
    /// <returns>The error message.</returns>
    public static MessageItem CreateErrorMessage<TEntity>(Expression<Func<TEntity, object?>> propertyExpression, LText? text = null) where TEntity : class
        => CreateErrorMessage(propertyExpression, null, text);

    /// <summary>
    /// Creates an error message using a property's JSON path and explicit serializer naming options.
    /// </summary>
    /// <typeparam name="TEntity">The root entity type.</typeparam>
    /// <param name="propertyExpression">The direct, nested, or indexed property expression.</param>
    /// <param name="options">The naming options, or null to use serializer defaults.</param>
    /// <param name="text">The message; null uses the standard invalid format and friendly metadata label.</param>
    /// <returns>The error message.</returns>
    public static MessageItem CreateErrorMessage<TEntity>(Expression<Func<TEntity, object?>> propertyExpression, JsonSerializerOptions? options, LText? text) where TEntity : class
    {
        var metadata = RuntimeMetadata.GetJsonPathAndText(propertyExpression, options);
        return text.HasValue ? CreateErrorMessage(metadata.Path, text.Value) : CreateErrorMessage(metadata.Path, Validation.Validation.InvalidFormat, metadata.Text);
    }

    /// <summary>
    /// Creates a new <see cref="MessageItem"/> with a specified <see cref="MessageType"/> and text.
    /// </summary>
    /// <param name="type">The <see cref="MessageType"/>.</param>
    /// <param name="text">The message text.</param>
    /// <returns>A <see cref="MessageItem"/>.</returns>
    public static MessageItem CreateMessage(MessageType type, LText text) => new() { Type = type, Text = text };

    /// <summary>
    /// Creates a new <see cref="MessageItem"/> with a specified <see cref="MessageType"/>, text format and and additional values included in the text.
    /// </summary>
    /// <param name="type">The <see cref="MessageType"/>.</param>
    /// <param name="format">The composite format string.</param>
    /// <param name="values">The values that form part of the message text.</param>
    /// <returns>A <see cref="MessageItem"/>.</returns>
    public static MessageItem CreateMessage(MessageType type, LText format, params IEnumerable<object?> values) => new() { Type = type, Text = format.EnsureNoArgs().WithArgs(values) };

    /// <summary>
    /// Creates a new <see cref="MessageItem"/> with the specified <see cref="Property"/>, <see cref="MessageType"/> and text.
    /// </summary>
    /// <param name="property">The property name.</param>
    /// <param name="type">The <see cref="MessageType"/>.</param>
    /// <param name="text">The message text.</param>
    /// <returns>A <see cref="MessageItem"/>.</returns>
    public static MessageItem CreateMessage(string? property, MessageType type, LText text) => new() { Property = property, Type = type, Text = text };

    /// <summary>
    /// Creates a new <see cref="MessageItem"/> with the specified <see cref="Property"/>, <see cref="MessageType"/>, text format and additional values included in the text.
    /// </summary>
    /// <param name="property">The property name.</param>
    /// <param name="type">The <see cref="MessageType"/>.</param>
    /// <param name="format">The composite format string.</param>
    /// <param name="values">The values that form part of the message text.</param>
    /// <returns>A <see cref="MessageItem"/>.</returns>
    public static MessageItem CreateMessage(string? property, MessageType type, LText format, params IEnumerable<object?> values)
        => new() { Property = property, Type = type, Text = format.EnsureNoArgs().WithArgs(values) };

    /// <summary>
    /// Creates a new <see cref="MessageType.Error"/> <see cref="MessageItem"/> with the specified <see cref="Property"/> and text.
    /// </summary>
    /// <param name="property">The property name.</param>
    /// <param name="text">The message text.</param>
    /// <returns>A <see cref="MessageItem"/>.</returns>
    public static MessageItem CreateErrorMessage(string? property, LText text) => new() { Property = property, Type = MessageType.Error, Text = text };

    /// <summary>
    /// Creates a new <see cref="MessageType.Error"/> <see cref="MessageItem"/> with the specified <see cref="Property"/>, text format and additional values included in the text.
    /// </summary>
    /// <param name="property">The property name.</param>
    /// <param name="format">The composite format string.</param>
    /// <param name="values">The values that form part of the message text.</param>
    /// <returns>A <see cref="MessageItem"/>.</returns>
    public static MessageItem CreateErrorMessage(string? property, LText format, params IEnumerable<object?> values)
        => new() { Property = property, Type = MessageType.Error, Text = format.EnsureNoArgs().WithArgs(values) };

    /// <summary>
    /// Creates a new <see cref="MessageType.Warning"/> <see cref="MessageItem"/> with the specified <see cref="Property"/> and text.
    /// </summary>
    /// <param name="property">The property name.</param>
    /// <param name="text">The message text.</param>
    /// <returns>A <see cref="MessageItem"/>.</returns>
    public static MessageItem CreateWarningMessage(string? property, LText text) => new() { Property = property, Type = MessageType.Warning, Text = text };

    /// <summary>
    /// Creates a new <see cref="MessageType.Warning"/> <see cref="MessageItem"/> with the specified <see cref="Property"/>, text format and additional values included in the text.
    /// </summary>
    /// <param name="property">The property name.</param>
    /// <param name="format">The composite format string.</param>
    /// <param name="values">The values that form part of the message text.</param>
    /// <returns>A <see cref="MessageItem"/>.</returns>
    public static MessageItem CreateWarningMessage(string? property, LText format, params IEnumerable<object?> values)
        => new() { Property = property, Type = MessageType.Warning, Text = format.EnsureNoArgs().WithArgs(values) };

    /// <summary>
    /// Creates a new <see cref="MessageType.Info"/> <see cref="MessageItem"/> with the specified <see cref="Property"/> and text.
    /// </summary>
    /// <param name="property">The property name.</param>
    /// <param name="text">The message text.</param>
    /// <returns>A <see cref="MessageItem"/>.</returns>
    public static MessageItem CreateInfoMessage(string? property, LText text) => new() { Property = property, Type = MessageType.Info, Text = text };

    /// <summary>
    /// Creates a new <see cref="MessageType.Info"/> <see cref="MessageItem"/> with the specified <see cref="Property"/>, text format and additional values included in the text.
    /// </summary>
    /// <param name="property">The property name.</param>
    /// <param name="format">The composite format string.</param>
    /// <param name="values">The values that form part of the message text.</param>
    /// <returns>A <see cref="MessageItem"/>.</returns>
    public static MessageItem CreateInfoMessage(string? property, LText format, params IEnumerable<object?> values)
        => new() { Property = property, Type = MessageType.Info, Text = format.EnsureNoArgs().WithArgs(values) };
}
