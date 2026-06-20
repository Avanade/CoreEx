namespace CoreEx.Entities;

/// <summary>
/// Specifies the intended writable state of an entity property, indicating whether it can be modified and under what conditions.
/// </summary>
/// <remarks>This is typically used to indicate the allowed operations for a property, such as whether it can be modified during creation, update, or both. It, however, does not enforce these rules;
/// it is merely descriptive. This is intended for the likes of OpenAPI for example, to mark up the properties as indicated.
/// <para>See also <see cref="Writable"/>.</para></remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class WritableAttribute(Writable writable = Writable.Always) : Attribute
{
    /// <summary>
    /// Gets the <see cref="Entities.Writable"/> intent.
    /// </summary>
    public Writable Writable { get; } = writable;
}