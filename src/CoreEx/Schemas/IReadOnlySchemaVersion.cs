namespace CoreEx.Schemas;

/// <summary>
/// Enables a read-only <see cref="SchemaVersion"/>.
/// </summary>
public interface IReadOnlySchemaVersion
{
    /// <summary>
    /// Gets the schema <see cref="Version"/>.
    /// </summary>
    string? SchemaVersion { get; }
}