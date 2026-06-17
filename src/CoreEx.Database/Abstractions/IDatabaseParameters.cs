namespace CoreEx.Database.Abstractions;

/// <summary>
/// Enables standardized access to the underlying <see cref="Parameters"/>.
/// </summary>
/// <typeparam name="TSelf">The owning <see cref="Type"/>.</typeparam>
public interface IDatabaseParameters<TSelf>
{
    /// <summary>
    /// Gets the <see cref="IDatabase"/>.
    /// </summary>
    IDatabase Database { get; }

    /// <summary>
    /// Gets the <see cref="DatabaseParameterCollection"/>.
    /// </summary>
    DatabaseParameterCollection Parameters { get; }
}