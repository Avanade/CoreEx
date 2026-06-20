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
}