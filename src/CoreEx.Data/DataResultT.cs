namespace CoreEx.Data;

/// <summary>
/// Represents the result of a data mutation with a <see cref="Value"/>; typically, a <see cref="OperationType.Create"/> or <see cref="OperationType.Update"/>.
/// </summary>
public readonly record struct DataResult<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataResult{T}"/> struct.
    /// </summary>
    /// <param name="value">The data operation result value.</param>
    /// <param name="wasMutated">Indicates whether the value was mutated; e.g. created, updated or deleted. </param>
    public DataResult(T value, bool wasMutated = true)
    {
        Value = value;
        WasMutated = wasMutated;
    }

    /// <summary>
    /// Gets the data operation result value.
    /// </summary>
    public T Value { get; }

    /// <summary>
    /// Indicates whether the value was mutated; e.g. created, updated or deleted. 
    /// </summary>
    /// <remarks>A <see langword="false"/> would be returned, for example, as follows:
    /// <list type="bullet">
    /// <item>A <see cref="OperationType.Delete"/> operation where the specified item to delete was not found.</item>
    /// <item>An <see cref="OperationType.Update"/> operation where no changes were made to the underlying data.</item>
    /// </list></remarks>
    public bool WasMutated { get; }

    /// <summary>
    /// Invokes the specified <paramref name="action"/> where the result <see cref="WasMutated"/> is <see langword="true"/>.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <returns>The <see cref="Value"/>.</returns>
    /// <remarks>This is a convenience method to allow fluent-style method-chaining.</remarks>
    public readonly T WhereMutated(Action<T> action)
    {
        if (WasMutated)
            action?.Invoke(Value);

        return Value;
    }

    /// <summary>
    /// Implicitly converts a <see cref="DataResult{T}"/> to a <see cref="Value"/>.
    /// </summary>
    /// <param name="result">The <see cref="DataResult{T}"/>.</param>
    /// <returns>The <see cref="Value"/>.</returns>
    public static implicit operator T(DataResult<T> result) => result.Value;
}