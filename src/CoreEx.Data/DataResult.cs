namespace CoreEx.Data;

/// <summary>
/// Represents the result of a data mutation without a value; typically, a <see cref="OperationType.Delete"/>.
/// </summary>
public readonly record struct DataResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataResult"/> struct.
    /// </summary>
    /// <param name="wasMutated">Indicates whether the value was mutated; e.g. created, updated or deleted. </param>
    public DataResult(bool wasMutated) => WasMutated = wasMutated;

    /// <summary>
    /// Gets a <see cref="DataResult"/> indicating <see cref="WasMutated"/> is <see langword="true"/>.
    /// </summary>
    public static DataResult True { get; } = new DataResult(true);

    /// <summary>
    /// Gets a <see cref="DataResult"/> indicating <see cref="WasMutated"/> is <see langword="false"/>.
    /// </summary>
    public static DataResult False { get; } = new DataResult(false);

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
    /// <remarks>This is a convenience method to allow fluent-style method-chaining.</remarks>
    public readonly void WhereMutated(Action action)
    {
        if (WasMutated)
            action?.Invoke();
    }

    /// <summary>
    /// Invokes the specified <paramref name="action"/> where the result <see cref="WasMutated"/> is <see langword="true"/> and returns the current <see cref="DataResult"/>.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <returns>The current <see cref="DataResult"/>.</returns>
    public readonly DataResult WhereMutatedAnd(Action action)
    {
        WhereMutated(action);
        return this;
    }

    /// <summary>
    /// Invokes the specified <paramref name="func"/> where the result <see cref="WasMutated"/> is <see langword="true"/>.
    /// </summary>
    /// <param name="func">The function to execute.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    public readonly Task WhereMutatedAsync(Func<CancellationToken, Task> func, CancellationToken cancellationToken = default)
    {
        if (WasMutated)
            return func?.Invoke(cancellationToken) ?? Task.CompletedTask;

        return Task.CompletedTask;
    }

    /// <summary>
    /// Invokes the specified <paramref name="func"/> where the result <see cref="WasMutated"/> is <see langword="true"/> and returns the current <see cref="DataResult"/>.
    /// </summary>
    /// <param name="func">The function to execute.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The current <see cref="DataResult"/>.</returns>
    public async readonly Task<DataResult> WhereMutatedAndAsync(Func<CancellationToken, Task> func, CancellationToken cancellationToken = default)
    {
        await WhereMutatedAsync(func, cancellationToken);
        return this;
    }
}
