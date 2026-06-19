namespace CoreEx.Validation.Abstractions;

/// <summary>
/// Enables a validation context for a property.
/// </summary>
public interface IPropertyContext
{
    /// <summary>
    /// Gets the owning entity <see cref="IValidationContext"/>.
    /// </summary>
    IValidationContext Owner { get; }

    /// <summary>
    /// Gets the property <see cref="IPropertyRuntimeMetadata"/>.
    /// </summary>
    IPropertyRuntimeMetadata Metadata { get; }

    /// <summary>
    /// Gets the additional parameters (see <see cref="ValidationArgs.Parameters"/>).
    /// </summary>
    IDictionary<string, object?> Parameters { get; }

    /// <summary>
    /// Gets the property name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the JSON property name.
    /// </summary>
    string JsonName { get; }

    /// <summary>
    /// Gets the fully qualified property name.
    /// </summary>
    string FullyQualifiedPropertyName { get; }

    /// <summary>
    /// Gets the fully qualified JSON property name.
    /// </summary>
    string FullyQualifiedJsonPropertyName { get; }

    /// <summary>
    /// Gets the friendly text.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    LText Text { get; }

    /// <summary>
    /// Gets the property value.
    /// </summary>
    object? Value { get; }

    /// <summary>
    /// Indicates whether the originating property <see cref="Value"/> was <see langword="null"/>.
    /// </summary>
    bool IsValueNull { get; }

    /// <summary>
    /// Indicates whether the originating property type is <see cref="Nullable{T}"/>.
    /// </summary>
    bool IsValueNullable { get; }

    /// <summary>
    /// Indicates whether the property is in error as a result of a previous validation.
    /// </summary>
    bool IsInError { get; }

    /// <summary>
    /// Gets the <see cref="Abstractions.ValueFormatter"/> to use when localizing the property value within an error message.
    /// </summary>
    ValueFormatter ValueFormatter { get; }

    /// <summary>
    /// Creates a new <see cref="MessageType.Error"/> <see cref="MessageItem"/> with the specified format and additional values to be included in the text and <b>adds</b> to the underlying <see cref="IValidationContext"/>.
    /// </summary>
    /// <param name="format">The composite format string.</param>
    /// <param name="values">The values that form part of the message text (<see cref="Text"/> and <see cref="Value"/> are automatically passed as the first two arguments to the string formatter).</param>
    /// <returns>A <see cref="MessageItem"/>.</returns>
    /// <remarks>The friendly <see cref="Text"/> and <see cref="Value"/> are automatically passed as the first two arguments to the string formatter.</remarks>
    MessageItem AddError(LText format, params object?[] values);

    /// <summary>
    /// Creates a fully qualified property name appending the <paramref name="name"/>.
    /// </summary>
    /// <param name="name">The property name.</param>
    string CreateFullyQualifiedPropertyName(string name);

    /// <summary>
    /// Creates a fully qualified JSON property name appending the <paramref name="jsonName"/>.
    /// </summary>
    /// <param name="jsonName">The JSON property name.</param>
    string CreateFullyQualifiedJsonPropertyName(string jsonName);
}