namespace CoreEx.Database.Mapping;

/// <summary>
/// Enables an <see cref="IDatabase"/> mapper.
/// </summary>
public interface IDatabaseMapper
{
    /// <summary>
    /// Gets the item <see cref="Type"/> being mapped from/to the database.
    /// </summary>
    Type ItemType { get; }

    /// <summary>
    /// Maps from a <paramref name="record"/> creating a corresponding instance of the <see cref="ItemType"/>.
    /// </summary>
    /// <param name="record">The <see cref="DatabaseRecord"/>.</param>
    /// <param name="operationType">The <see cref="OperationType"/> value being performed to enable conditional execution where appropriate.</param>
    /// <returns>The corresponding instance of the <see cref="ItemType"/>.</returns>
    object? MapFromDb(DatabaseRecord record, OperationType operationType = OperationType.Unspecified);

    /// <summary>
    /// Maps from a <paramref name="value"/> updating the <paramref name="parameters"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="parameters">The <see cref="DatabaseParameterCollection"/> to update from the <paramref name="value"/>.</param>
    /// <param name="operationType">The <see cref="OperationType"/> value being performed to enable conditional execution where appropriate.</param>
    void MapToDb(object? value, DatabaseParameterCollection parameters, OperationType operationType = OperationType.Unspecified);

    /// <summary>
    /// Maps the <paramref name="key"/> adding the corresponding <paramref name="parameters"/>.
    /// </summary>
    /// <param name="key">The primary <see cref="CompositeKey"/>.</param>
    /// <param name="parameters">The <see cref="DatabaseParameterCollection"/>.</param>
    /// <remarks>This is used to map only the key parameters; for example, used for a <b>Get</b> or <b>Delete</b> operation.</remarks>
    void MapKeyToDb(CompositeKey key, DatabaseParameterCollection parameters);
}