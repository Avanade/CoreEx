namespace CoreEx.Json;

/// <summary>
/// Defines the JSON filter option (either to <see cref="Include"/> or <see cref="Exclude"/> the specified paths) for the <see cref="JsonFilter"/>.
/// </summary>
public enum JsonFilterOption
{
    /// <summary>
    /// Indicates whether to <i>include</i> only those property paths that have been specified.
    /// </summary>
    Include,

    /// <summary>
    /// Indicates whether to <i>exclude</i> those property paths that have been specified.
    /// </summary>
    Exclude
}