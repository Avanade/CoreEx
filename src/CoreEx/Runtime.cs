namespace CoreEx;

/// <summary>
/// Provides standardized runtime utility capabilities.
/// </summary>
public static class Runtime
{
    /// <summary>
    /// Gets a <see cref="DateTimeOffset"/> value whose date and time are set to the current Coordinated Universal Time (UTC) date and time and whose offset is Zero, according to either the <see cref="ExecutionContext.Current"/>
    /// <see cref="ExecutionContext.Timestamp"/> where <see cref="ExecutionContext.HasCurrent"/>; otherwise, <see cref="TimeProvider.System"/> <see cref="TimeProvider.GetUtcNow"/>.
    /// </summary>
    public static DateTimeOffset UtcNow => ExecutionContext.TryGetCurrent(out var executionContext) ? executionContext.Timestamp : TimeProvider.System.GetUtcNow();

    /// <summary>
    /// Gets a new <see cref="Guid"/> value using the <see cref="IdentifierGenerator.Current"/> <see cref="IIdentifierGenerator"/>.
    /// </summary>
    /// <returns>A <see cref="Guid"/>.</returns>
    public static Guid NewGuid() => IdentifierGenerator.Current.GenerateGuid();

    /// <summary>
    /// Gets a new <see cref="Guid"/> value (see <see cref="NewGuid"/>) that is a formatted as a <see cref="string"/>.
    /// </summary>
    public static string NewId() => IdentifierGenerator.Current.GenerateGuid().ToString();
}