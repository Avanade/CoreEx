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
    /// <remarks><b>Note:</b> It is recommended that the <see cref="IdentifierGenerator.AssignIdentifierAsync{TFor}(TFor)"/> method is used for assigning identifiers to entities.</remarks>
    public static Guid NewGuid() => IdentifierGenerator.Current.GenerateGuid();

    /// <summary>
    /// Gets a new <see cref="Guid"/> value (see <see cref="NewGuid"/>) that is a formatted as a <see cref="string"/>.
    /// </summary>
    /// <remarks><b>Note:</b> It is recommended that the <see cref="IdentifierGenerator.AssignIdentifierAsync{TFor}(TFor)"/> method is used for assigning identifiers to entities.</remarks>
    public static string NewId() => IdentifierGenerator.Current.GenerateGuid().ToString();

    /// <summary>
    /// Gets a new identifier value of the specified type using the <see cref="IdentifierGenerator.Current"/> <see cref="IIdentifierGenerator"/>.
    /// </summary>
    /// <typeparam name="TId">The identifier <see cref="System.Type"/>.</typeparam>
    /// <returns>The newly generated identifier.</returns>
    public static Task<TId> GenerateIdentifierAsync<TId>() => IdentifierGenerator.Current.GenerateIdentifierAsync<TId>();

    /// <summary>
    /// Gets a new identifier value of the specified type for the specified entity type using the <see cref="IdentifierGenerator.Current"/> <see cref="IIdentifierGenerator"/>.
    /// </summary>
    /// <typeparam name="TId">The identifier <see cref="System.Type"/>.</typeparam>
    /// <typeparam name="TFor">The entity <see cref="System.Type"/>.</typeparam>
    /// <returns>The newly generated identifier.</returns>
    public static Task<TId> GenerateIdentifierAsync<TId, TFor>() where TFor : class => IdentifierGenerator.Current.GenerateIdentifierAsync<TId, TFor>();
}
