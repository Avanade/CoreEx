namespace CoreEx.Database.Abstractions;

/// <summary>
/// Provides <see cref="IDatabase"/> arguments.
/// </summary>
/// <remarks>The <see cref="DatabaseArgsBase"/> is intended, and expected, to be immutable. Therefore, when implementing/extending, please ensure additional properties are enabled as such to ensure there are
/// not any unintended side-effects.</remarks>
public abstract record class DatabaseArgsBase : IDataArgs
{
    /// <summary>
    /// Indicates whether the data should be refreshed (reselected where applicable) after a <b>save</b> operation.
    /// </summary>
    /// <remarks>Defaults to <see langword="false"/>.</remarks>
    public bool Refresh { get; init; } = false;

    /// <summary>
    /// Indicates whether to transform the underlying <see cref="DbException"/> into an <see cref="IExtendedException"/> equivalent.
    /// </summary>
    /// <remarks>Defaults to <see langword="true"/>.
    /// <para>The <see cref="Database{TConnection, TCommand, TDatabaseArgs, TDatabaseColumns}.OnDbException(DbException)"/> will be skipped where set to <see langword="false"/>.</para></remarks>
    public bool TransformException { get; init; } = true;

    /// <summary>
    /// Indicates whether to automatically retry the operation where the resulting exception is classified as transient (see <see cref="IDatabase.IsTransientException(Exception)"/>).
    /// </summary>
    /// <remarks>Defaults to <see langword="false"/> - retrying is opt-in per invocation as blind retry is only safe where the caller knows the underlying operation is safe to repeat (e.g. read-only,
    /// or otherwise idempotent). Where <see langword="true"/> and <see cref="RetryResiliencePipeline"/> is not specified, a default retry pipeline (3 attempts, exponential backoff, starting at 2 seconds)
    /// is used; see <see cref="Abstractions.DatabaseInvoker"/> for the invocation detail.
    /// <para>Only an exception already classified as transient is ever retried; any other exception is always rethrown immediately, unretried, before being passed to the existing
    /// <see cref="TransformException"/> handling.</para></remarks>
    public bool RetryOnTransient { get; init; } = false;

    /// <summary>
    /// Gets or sets an explicit <see cref="ResiliencePipeline{T}"/> (of <see cref="Result"/>) to use in place of the default retry pipeline, where <see cref="RetryOnTransient"/> is <see langword="true"/>.
    /// </summary>
    /// <remarks>Only consulted where <see cref="RetryOnTransient"/> is <see langword="true"/>; otherwise ignored. This governs only the retry timing/attempts/backoff - the classification of what is
    /// considered transient (and therefore ever retried) always remains <see cref="IDatabase.IsTransientException(Exception)"/>.</remarks>
    public ResiliencePipeline<Result>? RetryResiliencePipeline { get; init; }
}