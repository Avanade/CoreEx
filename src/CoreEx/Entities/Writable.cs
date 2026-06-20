namespace CoreEx.Entities;

/// <summary>
/// Represents the intended writable state of an entity property, indicating whether it can be modified and under what conditions.
/// </summary>
/// <remarks>This is typically used to indicate the allowed operations for a property, such as whether it can be modified during creation, update, or both. It, however, does not enforce these rules;
/// it is merely descriptive. This is intended for the likes of OpenAPI for example, to mark up the properties as indicated.
/// <para>See also <see cref="WritableAttribute"/>.</para></remarks>
public enum Writable
{
    /// <summary>
    /// The property is always writable, meaning it can be modified during both creation and update operations.
    /// </summary>
    Always = 0,

    /// <summary>
    /// The property is never writable, meaning it cannot be modified during either creation or update operations; is effectively read-only.
    /// </summary>
    Never = 1,

    /// <summary>
    /// The property is writable only during creation operations, meaning it can be set when an entity is created but cannot be modified during update operations.
    /// </summary>
    CreateOnly = 2,

    /// <summary>
    /// The property is writable only during update operations, meaning it cannot be set when an entity is created but can be modified during update operations.
    /// </summary>
    UpdateOnly = 3
}