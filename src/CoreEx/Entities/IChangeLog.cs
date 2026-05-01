namespace CoreEx.Entities;

/// <summary>
/// Enables a mutable <see cref="ChangeLog"/>.
/// </summary>
public interface IChangeLog : IReadOnlyChangeLog
{
    /// <inheritdoc/>
    ChangeLog? IReadOnlyChangeLog.ChangeLog => ChangeLog;

    /// <summary>
    /// Gets the <see cref="ChangeLog"/>.
    /// </summary>
    new ChangeLog? ChangeLog { get; set; }
}