namespace CoreEx.IO.Json;

/// <summary>Parses and composes literal-member JSON Pointer paths.</summary>
internal static class JsonPointer
{
    /// <summary>Parses an RFC 6901 pointer into decoded member names.</summary>
    /// <param name="pointer">The pointer to parse; the empty string selects the root.</param>
    /// <returns>The decoded path segments.</returns>
    public static string[] Parse(string pointer)
    {
        pointer.ThrowIfNull();
        if (pointer.Length == 0)
            return [];

        if (pointer[0] != '/')
            throw new ArgumentException("A JSON Pointer must be empty or begin with '/'.", nameof(pointer));

        var parts = pointer[1..].Split('/');
        for (var index = 0; index < parts.Length; index++)
        {
            var value = new System.Text.StringBuilder(parts[index].Length);
            for (var position = 0; position < parts[index].Length; position++)
            {
                var character = parts[index][position];
                if (character != '~')
                    value.Append(character);
                else if (++position < parts[index].Length && parts[index][position] is '0' or '1')
                    value.Append(parts[index][position] == '0' ? '~' : '/');
                else
                    throw new ArgumentException("A JSON Pointer contains an invalid escape.", nameof(pointer));
            }

            parts[index] = value.ToString();
        }

        return parts;
    }

    /// <summary>Appends one escaped object-member segment.</summary>
    /// <param name="parent">The parent JSON Pointer.</param>
    /// <param name="name">The decoded member name.</param>
    /// <returns>The child JSON Pointer.</returns>
    public static string Append(string parent, string name) => CoreEx.IO.Json.JsonMappingContext.Source(parent, name);

    /// <summary>Combines two already encoded pointers.</summary>
    /// <param name="parent">The parent pointer.</param>
    /// <param name="child">The child pointer, usually beginning with '/'.</param>
    /// <returns>The combined pointer.</returns>
    public static string Combine(string parent, string child) => child.Length == 0 ? parent : parent + child;

    /// <summary>Checks whether either decoded selector is equal to or nested within the other.</summary>
    /// <param name="left">The first selector.</param>
    /// <param name="right">The second selector.</param>
    /// <returns>Whether the selectors overlap.</returns>
    public static bool Overlaps(IReadOnlyList<string> left, IReadOnlyList<string> right)
    {
        var length = Math.Min(left.Count, right.Count);
        for (var index = 0; index < length; index++)
            if (!string.Equals(left[index], right[index], StringComparison.Ordinal))
                return false;

        return true;
    }
}
