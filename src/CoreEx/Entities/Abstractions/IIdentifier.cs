namespace CoreEx.Entities.Abstractions;

/// <summary>
/// Enables a mutable <see cref="Id"/> capability.
/// </summary>
public interface IIdentifier : IReadOnlyIdentifier
{
    /// <inheritdoc/>
    object? IIdentifierCore.Id => Id;

    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    new object? Id { get; set; }
}