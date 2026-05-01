namespace CoreEx.Database.Abstractions;

/// <summary>
/// Provides <see cref="IDatabase"/> arguments.
/// </summary>
/// <remarks>The <see cref="DatabaseArgs"/> is intended, and expected, to be immutable. Therefore, when implementing/extending, please ensure additional properties are enabled as such to ensure there are
/// not any unintended side-effects.</remarks>
public abstract record class DatabaseArgs : DatabaseArgsBase
{
    /// <summary>
    /// Gets or sets the <see cref="DbTransaction"/> <see cref="System.Data.IsolationLevel"/>.
    /// </summary>
    /// <remarks>Defaults to <see cref="IsolationLevel.ReadCommitted"/>.
    /// <para>This is used to specify the transaction isolation level for the likes of the <see cref="IUnitOfWork"/>.</para></remarks>
    public IsolationLevel IsolationLevel { get; init; } = IsolationLevel.ReadCommitted;
}