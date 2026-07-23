namespace CoreEx.Data.GraphQL.Internal;

/// <summary>
/// Converts a GraphQL AST <see cref="GraphQLValue"/> (resolving variable references) to a plain CLR <see cref="object"/>.
/// </summary>
internal static class GraphQLValueConverter
{
    /// <summary>
    /// Converts the specified <paramref name="value"/> to a plain CLR value, resolving <see cref="GraphQLVariable"/> references against <paramref name="variables"/>.
    /// </summary>
    public static object? Convert(GraphQLValue? value, IReadOnlyDictionary<string, object?>? variables) => value switch
    {
        null => null,
        GraphQLNullValue => null,
        GraphQLVariable variable => variables is not null && variables.TryGetValue(variable.Name.StringValue, out var v) ? FromJsonElement(v) : null,
        GraphQLStringValue str => str.Value.ToString(),
        GraphQLEnumValue enumValue => enumValue.Name.StringValue,
        GraphQLBooleanValue boolValue => boolValue.BoolValue,
        GraphQLIntValue intValue => int.TryParse(intValue.Value.Span, out var i) ? i : long.Parse(intValue.Value.Span),
        GraphQLFloatValue floatValue => double.Parse(floatValue.Value.Span),
        GraphQLListValue listValue => listValue.Values?.Select(v => Convert(v, variables)).ToList() ?? [],
        GraphQLObjectValue objValue => objValue.Fields?.ToDictionary(f => f.Name.StringValue, f => Convert(f.Value, variables), StringComparer.OrdinalIgnoreCase) ?? new Dictionary<string, object?>(),
        _ => throw new NotSupportedException($"Unsupported GraphQL value node kind '{value.Kind}'.")
    };

    /// <summary>
    /// Normalizes a variable value that may have been deserialized as a <see cref="JsonElement"/> (e.g. from a JSON request body) to a plain CLR value.
    /// </summary>
    private static object? FromJsonElement(object? value) => value is JsonElement element
        ? element.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.String => element.GetString(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : (object)element.GetDouble(),
            JsonValueKind.Array => element.EnumerateArray().Select(e => FromJsonElement(e)).ToList(),
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => FromJsonElement(p.Value), StringComparer.OrdinalIgnoreCase),
            _ => value
        }
        : value;

    /// <summary>
    /// Converts the field's <see cref="GraphQLArguments"/> to a plain CLR arguments dictionary.
    /// </summary>
    public static IReadOnlyDictionary<string, object?> ConvertArguments(GraphQLArguments? arguments, IReadOnlyDictionary<string, object?>? variables)
    {
        if (arguments?.Items is null || arguments.Items.Count == 0)
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var arg in arguments.Items)
            dict[arg.Name.StringValue] = Convert(arg.Value, variables);

        return dict;
    }

    /// <summary>
    /// Gets a named argument value as a <see cref="string"/> (or <see langword="null"/> where absent).
    /// </summary>
    public static string? GetString(this IReadOnlyDictionary<string, object?> args, string name) => args.TryGetValue(name, out var v) ? v?.ToString() : null;

    /// <summary>
    /// Gets a named argument value as an <see cref="int"/> (or <see langword="null"/> where absent).
    /// </summary>
    public static int? GetInt(this IReadOnlyDictionary<string, object?> args, string name)
    {
        if (!args.TryGetValue(name, out var v) || v is null)
            return null;

        return v switch { int i => i, long l => (int)l, string s when int.TryParse(s, out var r) => r, _ => null };
    }

    /// <summary>
    /// Gets a named argument value as a <see cref="bool"/> (or <see langword="null"/> where absent).
    /// </summary>
    public static bool? GetBool(this IReadOnlyDictionary<string, object?> args, string name)
    {
        if (!args.TryGetValue(name, out var v) || v is null)
            return null;

        return v switch { bool b => b, string s when bool.TryParse(s, out var r) => r, _ => null };
    }
}
