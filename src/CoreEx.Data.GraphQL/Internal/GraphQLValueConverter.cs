namespace CoreEx.Data.GraphQL.Internal;

/// <summary>
/// Converts a GraphQL AST <see cref="GraphQLValue"/> (resolving variable references) to a plain CLR <see cref="object"/>.
/// </summary>
internal static class GraphQLValueConverter
{
    /// <summary>
    /// Converts the specified <paramref name="value"/> to a plain CLR value, resolving <see cref="GraphQLVariable"/> references against <paramref name="variables"/>.
    /// </summary>
    /// <exception cref="GraphQLArgumentTranslationException">Thrown where <paramref name="value"/> references a <see cref="GraphQLVariable"/> that is not present in <paramref name="variables"/>,
    /// or is a malformed/out-of-range <c>Int</c> or <c>Float</c> literal.</exception>
    public static object? Convert(GraphQLValue? value, IReadOnlyDictionary<string, object?>? variables) => value switch
    {
        null => null,
        GraphQLNullValue => null,
        GraphQLVariable variable => variables is not null && variables.TryGetValue(variable.Name.StringValue, out var v)
            ? FromJsonElement(v)
            : throw new GraphQLArgumentTranslationException($"Variable '${variable.Name.StringValue}' was not provided."),
        GraphQLStringValue str => str.Value.ToString(),
        GraphQLEnumValue enumValue => enumValue.Name.StringValue,
        GraphQLBooleanValue boolValue => boolValue.BoolValue,
        GraphQLIntValue intValue => ParseInt(intValue),
        GraphQLFloatValue floatValue => ParseFloat(floatValue),
        GraphQLListValue listValue => listValue.Values?.Select(v => Convert(v, variables)).ToList() ?? [],
        GraphQLObjectValue objValue => objValue.Fields?.ToDictionary(f => f.Name.StringValue, f => Convert(f.Value, variables), StringComparer.OrdinalIgnoreCase) ?? new Dictionary<string, object?>(),
        _ => throw new NotSupportedException($"Unsupported GraphQL value node kind '{value.Kind}'.")
    };

    /// <summary>
    /// Parses an <c>Int</c> literal as an <see cref="int"/>, widening to <see cref="long"/> where it exceeds 32-bit range.
    /// </summary>
    /// <param name="intValue">The <c>Int</c> literal AST node.</param>
    /// <returns>The parsed <see cref="int"/> or <see cref="long"/> value.</returns>
    /// <exception cref="GraphQLArgumentTranslationException">Thrown where the literal cannot be parsed as either an <see cref="int"/> or a <see cref="long"/> (e.g. it exceeds 64-bit range).</exception>
    private static object ParseInt(GraphQLIntValue intValue)
    {
        if (int.TryParse(intValue.Value.Span, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
            return i;

        if (long.TryParse(intValue.Value.Span, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l))
            return l;

        throw new GraphQLArgumentTranslationException($"Int literal '{intValue.Value}' is not a valid 32-bit or 64-bit integer.");
    }

    /// <summary>
    /// Parses a <c>Float</c> literal as a <see cref="decimal"/> where it fits (retaining full base-10 precision for the common currency/measurement case), falling back to a
    /// <see cref="double"/> only where the literal exceeds <see cref="decimal"/>'s representable range.
    /// </summary>
    /// <param name="floatValue">The <c>Float</c> literal AST node.</param>
    /// <returns>The parsed <see cref="decimal"/> or <see cref="double"/> value.</returns>
    /// <exception cref="GraphQLArgumentTranslationException">Thrown where the literal cannot be parsed as either a <see cref="decimal"/> or a <see cref="double"/> (e.g. it exceeds the representable range).</exception>
    /// <remarks>Parsing straight to <see cref="double"/> unconditionally would silently truncate precision for a <c>decimal</c>-typed filter field - <see cref="double"/>'s
    /// ~15-17 significant digits is fewer than <see cref="decimal"/>'s 28-29 - substituting a subtly different value into the filter with no error.</remarks>
    private static object ParseFloat(GraphQLFloatValue floatValue)
    {
        if (decimal.TryParse(floatValue.Value.Span, NumberStyles.Float, CultureInfo.InvariantCulture, out var dec))
            return dec;

        return double.TryParse(floatValue.Value.Span, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)
            ? d
            : throw new GraphQLArgumentTranslationException($"Float literal '{floatValue.Value}' is not a valid floating-point number.");
    }

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
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.TryGetDecimal(out var dec) ? dec : (object)element.GetDouble(),
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
    /// <exception cref="GraphQLArgumentTranslationException">Thrown where the value is a <see cref="long"/> outside the range of a 32-bit integer, or is any other non-integer type/value
    /// (e.g. a non-numeric string or a wrong-typed variable) that cannot be coerced to an <see cref="int"/>.</exception>
    public static int? GetInt(this IReadOnlyDictionary<string, object?> args, string name)
    {
        if (!args.TryGetValue(name, out var v) || v is null)
            return null;

        return v switch
        {
            int i => i,
            long l => l is >= int.MinValue and <= int.MaxValue
                ? (int)l
                : throw new GraphQLArgumentTranslationException($"'{name}' value '{l}' is out of range for a 32-bit integer."),
            string s when int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var r) => r,
            _ => throw new GraphQLArgumentTranslationException($"'{name}' value '{v}' is not a valid integer.")
        };
    }

    /// <summary>
    /// Gets a named argument value as a <see cref="bool"/> (or <see langword="null"/> where absent).
    /// </summary>
    /// <exception cref="GraphQLArgumentTranslationException">Thrown where the value is any non-boolean type/value (e.g. a non-boolean string or a wrong-typed variable) that cannot be
    /// coerced to a <see cref="bool"/>.</exception>
    public static bool? GetBool(this IReadOnlyDictionary<string, object?> args, string name)
    {
        if (!args.TryGetValue(name, out var v) || v is null)
            return null;

        return v switch
        {
            bool b => b,
            string s when bool.TryParse(s, out var r) => r,
            _ => throw new GraphQLArgumentTranslationException($"'{name}' value '{v}' is not a valid boolean.")
        };
    }
}
