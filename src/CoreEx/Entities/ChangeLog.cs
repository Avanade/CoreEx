namespace CoreEx.Entities;

/// <summary>
/// Provides a <see cref="IReadOnlyChangeLogEx"/> implementation.
/// </summary>
public record class ChangeLog() : IReadOnlyChangeLogEx, IRuntimeMetadata, IDefault
{
    private static readonly ChangeLog _empty = new();

    /// <summary>
    /// Gets the username and timestamp information for <see cref="ChangeLog"/> usage.
    /// </summary>
    /// <param name="executionContext">The optional <see cref="ExecutionContext"/>.</param>
    /// <returns>The user and timestamp information.</returns>
    public static (string? UserName, DateTimeOffset Timestamp) GetChangeLogInfo(ExecutionContext? executionContext = null)
    {
        if (executionContext is null)
            ExecutionContext.TryGetCurrent(out executionContext);

        return (executionContext?.User?.UserName ?? AuthenticationUser.EnvironmentUser.UserName, executionContext?.Timestamp ?? Runtime.UtcNow);
    }

    /// <summary>
    /// Creates a new <see cref="ChangeLog"/> setting the <see cref="ChangeLog.CreatedBy"/> and <see cref="ChangeLog.CreatedOn"/>.
    /// </summary>
    /// <param name="executionContext">The optional <see cref="ExecutionContext"/>.</param>
    /// <returns>A <see cref="ChangeLog"/>.</returns>
    public static ChangeLog CreateCreated(ExecutionContext? executionContext = null)
    {
        var (UserName, Timestamp) = GetChangeLogInfo(executionContext);

        return new ChangeLog
        {
            CreatedBy = UserName,
            CreatedOn = Timestamp
        };
    }

    /// <summary>
    /// Creates a <see cref="ChangeLog"/> copy (or new) setting the <see cref="ChangeLog.UpdatedBy"/> and <see cref="ChangeLog.UpdatedOn"/>.
    /// </summary>
    /// <param name="changeLog">The optional <see cref="ChangeLog"/> to copy from.</param>
    /// <param name="executionContext">The optional <see cref="ExecutionContext"/>.</param>
    /// <returns>A <see cref="ChangeLog"/>.</returns>
    public static ChangeLog CreateChanged(ChangeLog? changeLog = null, ExecutionContext? executionContext = null)
    {
        var (UserName, Timestamp) = GetChangeLogInfo(executionContext);

        return new ChangeLog
        {
            CreatedBy = changeLog?.CreatedBy,
            CreatedOn = changeLog?.CreatedOn,
            UpdatedBy = UserName,
            UpdatedOn = Timestamp
        };
    }

    /// <summary>
    /// Creates a new <see cref="ChangeLog"/> from an <see cref="IReadOnlyChangeLogEx"/>.
    /// </summary>
    /// <param name="changeLog">The <see cref="IReadOnlyChangeLogEx"/>.</param>
    /// <returns>The <see cref="ChangeLog"/> where the result is not <see cref="IsDefault"/>; otherwise, <see langword="null"/>.</returns>
    public static ChangeLog? CreateFrom(IReadOnlyChangeLogEx? changeLog)
    {
        var cl = new ChangeLog(changeLog);
        return cl.IsDefault() ? null : cl;
    }

    /// <inheritdoc/>
    public static IEnumerable<IPropertyRuntimeMetadata> GetStaticPropertyRuntimeMetadata()
    {
        yield return new PropertyRuntimeMetadata<ChangeLog, string?>(nameof(CreatedBy), static e => e.CreatedBy, clean: CleanOption.CleanAndDefault);
        yield return new PropertyRuntimeMetadata<ChangeLog, DateTimeOffset?>(nameof(CreatedOn), static e => e.CreatedOn, clean: CleanOption.CleanAndDefault);
        yield return new PropertyRuntimeMetadata<ChangeLog, string?>(nameof(UpdatedBy), static e => e.UpdatedBy, clean: CleanOption.CleanAndDefault);
        yield return new PropertyRuntimeMetadata<ChangeLog, DateTimeOffset?>(nameof(UpdatedOn), static e => e.UpdatedOn, clean: CleanOption.CleanAndDefault);
    }

    /// <summary>
    /// Gets an empty <see cref="ChangeLog"/> instance.
    /// </summary>
    public static ChangeLog Empty => _empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChangeLog"/> class with an <see cref="IReadOnlyChangeLog"/>.
    /// </summary>
    /// <param name="changeLog">The <see cref="IReadOnlyChangeLog"/>.</param>
    public ChangeLog(IReadOnlyChangeLog? changeLog) : this((IReadOnlyChangeLogEx?)changeLog?.ChangeLog) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChangeLog"/> class.
    /// </summary>
    /// <param name="changeLog">The optional <see cref="IReadOnlyChangeLogEx"/>.</param>
    public ChangeLog(IReadOnlyChangeLogEx? changeLog) : this()
    {
        if (changeLog is not null)
        {
            CreatedBy = changeLog.CreatedBy;
            CreatedOn = changeLog.CreatedOn;
            UpdatedBy = changeLog.UpdatedBy;
            UpdatedOn = changeLog.UpdatedOn;
        }
    }

    /// <inheritdoc/>
    [ReadOnly(true)]
    public string? CreatedBy { get; init => field = Cleaner.Clean(value); }

    /// <inheritdoc/>
    [ReadOnly(true)]
    public DateTimeOffset? CreatedOn { get; init; }

    /// <inheritdoc/>
    [ReadOnly(true)]
    public string? UpdatedBy { get; init => field = Cleaner.Clean(value); }

    /// <inheritdoc/>
    [ReadOnly(true)]
    public DateTimeOffset? UpdatedOn { get; init; }

    /// <inheritdoc/>
    public virtual IEnumerable<IPropertyRuntimeMetadata> GetPropertyRuntimeMetadata()
    {
        foreach (var pr in GetStaticPropertyRuntimeMetadata())
            yield return pr;
    }

    /// <inheritdoc/>
    public bool IsDefault() => RuntimeMetadata.IsDefault(this);
}