namespace CoreEx.Database.Mapping;

/// <summary>
/// Provides a <see cref="IDatabaseMapper{TItem}"/> using optional mapping functions.
/// </summary>
/// <typeparam name="TItem">The resulting item <see cref="Type"/>.</typeparam>
/// <param name="mapFromDb">The optional <see cref="DatabaseRecord"/> to <typeparamref name="TItem"/> mapping function.</param>
/// <param name="mapToDb">The optional <typeparamref name="TItem"/> to <see cref="DatabaseParameterCollection"/> mapping action.</param>
/// <remarks>The <see cref="MapFromDb(CoreEx.Database.DatabaseRecord, CoreEx.OperationType)"/> and <see cref="MapToDb(TItem?, CoreEx.Database.DatabaseParameterCollection, CoreEx.OperationType)"/>
/// methods will throw a <see cref="NotImplementedException"/> unless overridden or provided via the constructor.</remarks>
public class DatabaseMapper<TItem>(Func<DatabaseRecord, OperationType, TItem>? mapFromDb = null, Action<TItem?, DatabaseParameterCollection, OperationType>? mapToDb = null) : IDatabaseMapper<TItem>
{
    private readonly Func<DatabaseRecord, OperationType, TItem>? _mapFromDb = mapFromDb;
    private readonly Action<TItem?, DatabaseParameterCollection, OperationType>? _mapToDb = mapToDb;

    /// <inheritdoc/>
    public virtual TItem MapFromDb(DatabaseRecord record, OperationType operationType) => (_mapFromDb ?? throw new NotImplementedException())(record, operationType);

    /// <inheritdoc/>
    public virtual void MapToDb(TItem? value, DatabaseParameterCollection parameters, OperationType operationType) => (_mapToDb ?? throw new NotImplementedException())(value, parameters, operationType);
}