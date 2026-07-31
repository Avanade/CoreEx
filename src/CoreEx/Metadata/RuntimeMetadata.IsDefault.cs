namespace CoreEx.Metadata;

public static partial class RuntimeMetadata
{
    [ThreadStatic]
    private static HashSet<object>? _visitingForIsDefault;

    /// <summary>
    /// Indicates whether the <paramref name="value"/> is in its default state.
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true"/> indicates that the value is considered default; otherwise, <see langword="false"/>.
    /// <para>See also <see cref="IDefault"/>.</para></returns>
    /// <remarks>This will leverage either the underlying<see cref="IRuntimeMetadataCore"/> implementation or reflection (<see cref="GetCachedProperties{T}()"/>) depending on the types.</remarks>
    public static bool IsDefault<T>(T value) => IsDefault(value, default!);

    /// <summary>
    /// Indicates whether the <paramref name="value"/> is in its default state.
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    /// <param name="value">The value.</param>
    /// <param name="default">The default value (used where applicable).</param>
    /// <returns><see langword="true"/> indicates that the value is considered default; otherwise, <see langword="false"/>.</returns>
    internal static bool IsDefault<T>(T value, T @default)
    {
        if (value == null)
            return true;

        if (value is string str)
            return AreEqual(str, Internal.Cast<T, string>(@default));

        if (value is IRuntimeMetadataCore rm)
        {
            var set = _visitingForIsDefault ??= new HashSet<object>(ReferenceEqualityComparer.Instance);
            var isRoot = set.Count == 0;
            try
            {
                if (!set.Add(value))
                    return false; // cycle detected — a self-referential object is not considered default

                bool result = !rm.GetPropertyRuntimeMetadata().Any(x => !x.IsDefault(value));
                set.Remove(value);
                return result;
            }
            finally
            {
                if (isRoot)
                    set.Clear();
            }
        }

        if (value is ICollection ic && ic.Count == 0)
            return true;

        var type = value.GetType();
        if (type.IsValueType)
            return AreEqual(value, @default);

        {
            var set = _visitingForIsDefault ??= new HashSet<object>(ReferenceEqualityComparer.Instance);
            var isRoot = set.Count == 0;
            try
            {
                if (!set.Add(value))
                    return false; // cycle detected

                bool result = !GetPropertyRuntimeMetadata(type).Any(x => !x.IsDefault(value));
                set.Remove(value);
                return result;
            }
            finally
            {
                if (isRoot)
                    set.Clear();
            }
        }
    }
}
