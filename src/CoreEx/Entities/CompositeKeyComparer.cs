namespace CoreEx.Entities;

/// <summary>
/// Represents a comparer of equality for a <see cref="CompositeKey"/>.
/// </summary>
public class CompositeKeyComparer : IEqualityComparer<CompositeKey>
{
    private static readonly CompositeKeyComparer _default = new();
    private static readonly object _nothing = new();

    /// <summary>
    /// Gets the default <see cref="CompositeKeyComparer"/> instance.
    /// </summary>
    public static CompositeKeyComparer Default => _default;

    /// <summary>
    /// Determines whether the specified values are equal from a <see cref="CompositeKey"/> perspective.
    /// </summary>
    /// <param name="x">The first object to compare.</param>
    /// <param name="y">The second object to compare.</param>
    /// <returns><see langword="true"/> if the values are equal; otherwise, <see langword="false"/>.</returns>
    /// <remarks>This method supports comparing both <see cref="CompositeKey"/> and <see cref="IEntityKey"/> values.</remarks>
    public new bool Equals(object? x, object? y)
    {
        if (x is CompositeKey ckx && y is CompositeKey cky)
            return Equals(ckx, cky);
        else if (x is IEntityKey ekx && y is IEntityKey eky)
            return Equals(ekx.EntityKey, eky.EntityKey);
        else
            return false;
    }

    /// <summary>
    /// Determines whether the specified <see cref="CompositeKey"/> values are equal.
    /// </summary>
    /// <param name="x">The first <see cref="CompositeKey"/> to compare.</param>
    /// <param name="y">The second <see cref="CompositeKey"/> to compare.</param>
    /// <returns><see langword="true"/> if the values are equal; otherwise, <see langword="false"/>.</returns>
    public bool Equals(CompositeKey x, CompositeKey y)
    {
        // Fast path optimization: compare tuples directly without boxing
        if (x.IsFastPath && y.IsFastPath)
        {
            if (x.FastPathCount != y.FastPathCount)
                return false;

            if (x.FastPath is not ITuple tupleX || y.FastPath is not ITuple tupleY)
                return false;

            for (int i = 0; i < x.FastPathCount; i++)
            {
                if (!GetArgValue(tupleX[i]).Equals(GetArgValue(tupleY[i])))
                    return false;
            }

            return true;
        }

        // Fallback to Args comparison (materializes if needed)
        if (x.Args == null && y.Args == null)
            return true;
        else if (x.Args == null || y.Args == null)
            return false;
        else if (x.Args!.Length != y.Args!.Length)
            return false;

        for (int i = 0; i < x.Args.Length; i++)
        {
            if (!GetArgValue(x.Args[i]).Equals(GetArgValue(y.Args[i])))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Returns a hash code for the specified value.
    /// </summary>
    /// <param name="value">The value for which a hash code is to be returned.</param>
    /// <returns>A hash code for the <see cref="CompositeKey"/>.</returns>
    /// <remarks>This method supports both <see cref="CompositeKey"/> and <see cref="IEntityKey"/> values.</remarks>
    public int GetHashCode(object? value)
    {
        if (value is CompositeKey key)
            return GetHashCode(key);
        else if (value is IEntityKey entityKey)
            return GetHashCode(entityKey.EntityKey);
        else
            return value is null ? _nothing.GetHashCode() : value.GetHashCode();
    }

    /// <summary>
    /// Returns a hash code for the <see cref="CompositeKey"/>.
    /// </summary>
    /// <param name="key">The <see cref="CompositeKey"/> for which a hash code is to be returned.</param>
    /// <returns>A hash code for the <see cref="CompositeKey"/>.</returns>
    public int GetHashCode(CompositeKey key)
    {
        // Fast path optimization: compute hash without boxing
        if (key.IsFastPath)
        {
            if (key.FastPath is not ITuple tuple)
                return 0;

            return key.FastPathCount switch
            {
                0 => 0,
                1 => GetArgValue(tuple[0]).GetHashCode(),
                2 => HashCode.Combine(GetArgValue(tuple[0]), GetArgValue(tuple[1])),
                3 => HashCode.Combine(GetArgValue(tuple[0]), GetArgValue(tuple[1]), GetArgValue(tuple[2])),
                4 => HashCode.Combine(GetArgValue(tuple[0]), GetArgValue(tuple[1]), GetArgValue(tuple[2]), GetArgValue(tuple[3])),
                _ => 0
            };
        }

        // Fallback to Args (materializes if needed)
        if (key.Args.Length == 0)
            return 0;

        if (key.Args.Length == 1)
            return GetArgValue(key.Args[0]).GetHashCode();

        var hashCode = new HashCode();
        for (int i = 0; i < key.Args.Length; i++)
            hashCode.Add(GetArgValue(key.Args[i]));

        return hashCode.ToHashCode();
    }

    /// <summary>
    /// Gets the argument value or nothing.
    /// </summary>
    private static object GetArgValue(object? arg) => arg ?? _nothing;
}