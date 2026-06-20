namespace CoreEx.Database.Mapping;

/// <summary>
/// Enables an <see cref="IDatabase"/> mapper.
/// </summary>
/// <typeparam name="TItem">The <see cref="IDatabaseMapper.ItemType"/>.</typeparam>
public interface IDatabaseMapper<TItem> : IDatabaseMapper
{
    /// <inheritdoc/>
    Type IDatabaseMapper.ItemType => typeof(TItem);

    /// <inheritdoc/>
    object? IDatabaseMapper.MapFromDb(DatabaseRecord record, OperationType operationType) => MapFromDb(record, operationType)!;

    /// <inheritdoc/>
    void IDatabaseMapper.MapToDb(object? value, DatabaseParameterCollection parameters, OperationType operationType) => MapToDb((TItem?)value, parameters, operationType);

    /// <summary>
    /// Maps from a <paramref name="record"/> creating a corresponding instance of <typeparamref name="TItem"/>.
    /// </summary>
    /// <param name="record">The <see cref="DatabaseRecord"/>.</param>
    /// <param name="operationType">The <see cref="OperationType"/> value being performed to enable conditional execution where appropriate.</param>
    /// <returns>The corresponding instance of <typeparamref name="TItem"/>.</returns>
    new TItem? MapFromDb(DatabaseRecord record, OperationType operationType = OperationType.Unspecified);

    /// <summary>
    /// Maps from a <paramref name="value"/> updating the <paramref name="parameters"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="parameters">The <see cref="DatabaseParameterCollection"/> to update from the <paramref name="value"/>.</param>
    /// <param name="operationType">The <see cref="OperationType"/> value being performed to enable conditional execution where appropriate.</param>
    void MapToDb(TItem? value, DatabaseParameterCollection parameters, OperationType operationType = OperationType.Unspecified);

    /// <inheritdoc/>
    void IDatabaseMapper.MapKeyToDb(CompositeKey key, DatabaseParameterCollection parameters) => throw new NotSupportedException();
}