namespace CoreEx.Schemas;

/// <summary>
/// Enables a mutable <see cref="SchemaVersion"/>.
/// </summary>
public interface ISchemaVersion : IReadOnlySchemaVersion
{
    /// <inheritdoc/>
    string? IReadOnlySchemaVersion.SchemaVersion => SchemaVersion;

    /// <summary>
    /// Gets or sets the schema <see cref="Version"/>.
    /// </summary>
    new string? SchemaVersion { get; set; }
}