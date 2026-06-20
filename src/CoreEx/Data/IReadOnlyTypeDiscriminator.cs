namespace CoreEx.Data;

/// <summary>
/// Enables a read-only <see cref="TypeDiscriminator"/> to identify the underlying type of the data model.
/// </summary>
public interface IReadOnlyTypeDiscriminator
{
    /// <summary>
    /// Gets the type discriminator name.
    /// </summary>
    /// <remarks>This defaults to the corresponding <see cref="SchemaAttribute.Name"/>; otherwise, the underlying <see cref="Type"/> <see cref="MemberInfo.Name"/>.</remarks>
    string? TypeDiscriminator { get; }
}