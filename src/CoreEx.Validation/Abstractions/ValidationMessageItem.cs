namespace CoreEx.Validation.Abstractions;

/// <summary>
/// Represents a validation-extended <see cref="MessageItem"/>.
/// </summary>
public record class ValidationMessageItem : MessageItem
{
    /// <summary>
    /// Gets the fully qualified property name that the message relates to.
    /// </summary>
    /// <remarks>Required for internal validation purposes; specifically for tracking which property a validation message is associated with as the <see cref="MessageItem.Property"/> may contain the
    /// fully qualified JSON name.</remarks>
    [JsonIgnore]
    internal string? FullyQualifiedPropertyName { get; set; }
}