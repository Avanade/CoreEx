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
        var xArgs = x.Args;
        var yArgs = y.Args;
        if (xArgs.Length != yArgs.Length)
            return false;

        for (int i = 0; i < xArgs.Length; i++)
        {
            if (!ArgEquals(xArgs[i], yArgs[i]))
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
        var args = key.Args;
        if (args.Length == 0)
            return 0;

        if (args.Length == 1)
            return ArgHash(args[0]);

        var hashCode = new HashCode();
        for (int i = 0; i < args.Length; i++)
            hashCode.Add(ArgHash(args[i]));

        return hashCode.ToHashCode();
    }
    /// <summary>
    /// Determines whether the arguments are equal; a <see cref="byte"/> array is compared by content.
    /// </summary>
    private static bool ArgEquals(object? x, object? y) => x is byte[] bx && y is byte[] by ? bx.AsSpan().SequenceEqual(by) : (x ?? _nothing).Equals(y ?? _nothing);

    /// <summary>
    /// Gets the argument hash code; a <see cref="byte"/> array is hashed by content.
    /// </summary>
    private static int ArgHash(object? arg)
    {
        if (arg is not byte[] bytes)
            return (arg ?? _nothing).GetHashCode();

        var hashCode = new HashCode();
        hashCode.AddBytes(bytes);
        return hashCode.ToHashCode();
    }
}
