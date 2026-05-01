namespace CoreEx.Metadata;

public static partial class RuntimeMetadata
{
    /// <summary>
    /// Gets the hash code for the <paramref name="value"/>.
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    /// <param name="value">The value.</param>
    /// <returns>A 32-bit signed integer hash code.</returns>
    public static int GetHashCode<T>(T? value)
    {
        switch (value)
        {
            case null:
                return 0;

            case string s:
                return s.GetHashCode();

            case IRuntimeMetadataCore rm:
                {
                    var hash = new HashCode();
                    foreach (var p in rm.GetPropertyRuntimeMetadata())
                    {
                        hash.Add(GetHashCode(p.GetValue(value)));
                    }

                    return hash.ToHashCode();
                }

            case IDictionary d:
                {
                    var hash = new HashCode();
                    var e = d.GetEnumerator();
                    while (e.MoveNext())
                    {
                        hash.Add(GetHashCode(e.Key));
                        hash.Add(GetHashCode(e.Value));
                    }

                    return hash.ToHashCode();
                }

            case IEnumerable e:
                {
                    var hash = new HashCode();
                    foreach (var item in e)
                        hash.Add(GetHashCode(item));

                    return hash.ToHashCode();
                }

            default:
                {
                    var type = value.GetType();
                    if (type.IsValueType)
                        return value.GetHashCode();

                    // Must be a class so use reflection-based runtime-metadata.
                    var hash = new HashCode();
                    foreach (var p in GetCachedProperties(type).Values)
                    {
                        hash.Add(GetHashCode(p.GetValue(value)));
                    }

                    return hash.ToHashCode();
                }
        }
    }
}