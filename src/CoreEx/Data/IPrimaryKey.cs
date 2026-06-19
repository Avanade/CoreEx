namespace CoreEx.Data;

/// <summary>
/// Enables the read-only <see cref="PrimaryKey"/>.
/// </summary>
public interface IPrimaryKey : IEntityKey
{
    /// <summary>
    /// Gets the <i>primary key</i> (represented as a <see cref="CompositeKey"/>).
    /// </summary>
    [JsonIgnore]
    CompositeKey PrimaryKey { get; }

    /// <inheritdoc/>
    [JsonIgnore]
    CompositeKey IEntityKey.EntityKey => PrimaryKey;
}