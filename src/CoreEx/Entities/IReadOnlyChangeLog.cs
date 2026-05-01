namespace CoreEx.Entities;

/// <summary>
/// Enables a read-only <see cref="ChangeLog"/>.
/// </summary>
public interface IReadOnlyChangeLog
{
    /// <summary>
    /// Gets the <see cref="ChangeLog"/>.
    /// </summary>
    ChangeLog? ChangeLog { get; }
}