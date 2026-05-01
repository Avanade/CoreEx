namespace CoreEx.Data;

/// <summary>
/// Enables a mutable <see cref="TypeDiscriminator"/> to identify the underlying type of the data model.
/// </summary>
public interface ITypeDiscriminator : IReadOnlyTypeDiscriminator
{
    /// <inheritdoc/>
    string? IReadOnlyTypeDiscriminator.TypeDiscriminator => TypeDiscriminator;

    /// <summary>
    /// Gets or sets the type discriminator name.
    /// </summary>
    /// <remarks>This defaults to the corresponding <see cref="SchemaAttribute.Name"/>; otherwise, the underlying <see cref="Type"/> <see cref="MemberInfo.Name"/>.</remarks>
    new string? TypeDiscriminator { get; set; }
}