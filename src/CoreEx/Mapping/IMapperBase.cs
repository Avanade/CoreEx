namespace CoreEx.Mapping;

/// <summary>
/// Enables base capabilities to support mapping from a <see cref="SourceType"/> to a <see cref="DestinationType"/> value.
/// </summary>
public interface IMapperBase
{
    /// <summary>
    /// Gets the source <see cref="Type"/>.
    /// </summary>
    Type SourceType { get; }

    /// <summary>
    /// Gets the destination <see cref="Type"/>.
    /// </summary>
    Type DestinationType { get; }
}