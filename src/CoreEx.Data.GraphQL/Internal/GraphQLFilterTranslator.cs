namespace CoreEx.Data.GraphQL.Internal;

/// <summary>
/// Translates the GraphQL-native <c>where</c> argument (a structured input object using the same operator vocabulary as <see cref="QueryFilterOperator"/>) into the equivalent
/// OData-esque filter <see cref="string"/> consumed by <see cref="QueryFilterParser"/>.
/// </summary>
/// <remarks>The <c>where</c> shape mirrors mainstream GraphQL filtering conventions (e.g. Hot Chocolate, Prisma): a field-keyed object whose value is either an operator-keyed
/// object (<c>{ sku: { startsWith: "spec" } }</c>) or a bare scalar shorthand for equality (<c>{ sku: "ABC" }</c> ≡ <c>{ sku: { eq: "ABC" } }</c>); <c>and</c>/<c>or</c>/<c>not</c>
/// keys compose nested <c>where</c> objects. Operator keys (<c>eq</c>, <c>ne</c>, <c>gt</c>, <c>ge</c>, <c>lt</c>, <c>le</c>, <c>in</c>, <c>startsWith</c>, <c>endsWith</c>,
/// <c>contains</c>) are a 1:1, case-insensitive rename of <see cref="QueryFilterOperator"/> — <b>exact</b> compatibility with the existing REST <c>$filter</c> vocabulary.
/// <para>This translator performs <b>no</b> field/operator legality validation of its own — by design, the resulting OData-esque string is always passed through the existing,
/// unmodified <see cref="QueryFilterParser"/>, so any unsupported field or operator combination still fails safely as a standard <see cref="QueryFilterParserException"/>
/// (defense in depth); this translator is responsible for syntax rewriting only.</para></remarks>
internal static class GraphQLFilterTranslator
{
    /// <summary>
    /// Translates the resolved <c>where</c> argument value to the equivalent OData-esque filter string.
    /// </summary>
    /// <param name="where">The resolved <c>where</c> argument value (an <see cref="IReadOnlyDictionary{TKey, TValue}"/> produced by <see cref="GraphQLValueConverter"/>, or
    /// <see langword="null"/> where not specified).</param>
    /// <returns>The OData-esque filter string (or <see langword="null"/> where <paramref name="where"/> is <see langword="null"/>).</returns>
    public static string? Translate(object? where) => where is null ? null : TranslateNode(where, "where");

    /// <summary>
    /// Recursively translates a single <c>where</c> input object (or a nested <c>and</c>/<c>or</c>/<c>not</c> member) to its OData-esque expression.
    /// </summary>
    /// <param name="node">The input object node (expected to be an <see cref="IReadOnlyDictionary{TKey, TValue}"/>).</param>
    /// <param name="path">The GraphQL argument path, used for translation error messages.</param>
    /// <returns>The OData-esque expression fragment.</returns>
    private static string TranslateNode(object? node, string path)
    {
        if (node is not IReadOnlyDictionary<string, object?> dict)
            throw new GraphQLArgumentTranslationException($"'{path}' must be an input object.");

        var clauses = new List<string>();
        foreach (var (key, value) in dict)
        {
            if (string.Equals(key, "and", StringComparison.OrdinalIgnoreCase) || string.Equals(key, "or", StringComparison.OrdinalIgnoreCase))
            {
                if (value is not IEnumerable<object?> list)
                    throw new GraphQLArgumentTranslationException($"'{path}.{key}' must be a list of input objects.");

                var items = list.Select((item, i) => TranslateNode(item, $"{path}.{key}[{i}]")).ToList();
                if (items.Count > 0)
                    clauses.Add($"({string.Join($" {key.ToLowerInvariant()} ", items)})");

                continue;
            }

            if (string.Equals(key, "not", StringComparison.OrdinalIgnoreCase))
            {
                clauses.Add($"not ({TranslateNode(value, $"{path}.not")})");
                continue;
            }

            clauses.Add(TranslateFieldClause(key, value, path));
        }

        return clauses.Count switch
        {
            0 => throw new GraphQLArgumentTranslationException($"'{path}' must specify at least one field or logical operator."),
            1 => clauses[0],
            _ => $"({string.Join(" and ", clauses)})"
        };
    }

    /// <summary>
    /// Translates a single field clause (either an operator-keyed object or a bare scalar equality shorthand) to its OData-esque expression.
    /// </summary>
    /// <param name="field">The GraphQL field name.</param>
    /// <param name="value">The field's value (an operator-keyed object, or a scalar/list shorthand).</param>
    /// <param name="path">The parent GraphQL argument path, used for translation error messages.</param>
    /// <returns>The OData-esque expression fragment.</returns>
    private static string TranslateFieldClause(string field, object? value, string path)
    {
        if (value is not IReadOnlyDictionary<string, object?> operators)
            return $"{field} eq {FormatValue(value, $"{path}.{field}")}"; // Bare scalar shorthand for equality.

        var clauses = operators.Select(op => TranslateOperator(field, op.Key, op.Value, $"{path}.{field}")).ToList();
        return clauses.Count == 1 ? clauses[0] : $"({string.Join(" and ", clauses)})";
    }

    /// <summary>
    /// Translates a single field/operator pair to its OData-esque expression.
    /// </summary>
    /// <param name="field">The GraphQL field name.</param>
    /// <param name="operatorName">The operator key (e.g. <c>eq</c>, <c>startsWith</c>).</param>
    /// <param name="value">The operator's operand value.</param>
    /// <param name="path">The parent GraphQL argument path, used for translation error messages.</param>
    /// <returns>The OData-esque expression fragment.</returns>
    private static string TranslateOperator(string field, string operatorName, object? value, string path) => operatorName.ToLowerInvariant() switch
    {
        "eq" => $"{field} eq {FormatValue(value, $"{path}.eq")}",
        "ne" => $"{field} ne {FormatValue(value, $"{path}.ne")}",
        "gt" => $"{field} gt {FormatValue(value, $"{path}.gt")}",
        "ge" => $"{field} ge {FormatValue(value, $"{path}.ge")}",
        "lt" => $"{field} lt {FormatValue(value, $"{path}.lt")}",
        "le" => $"{field} le {FormatValue(value, $"{path}.le")}",
        "startswith" => $"startswith({field}, {FormatValue(value, $"{path}.startsWith")})",
        "endswith" => $"endswith({field}, {FormatValue(value, $"{path}.endsWith")})",
        "contains" => $"contains({field}, {FormatValue(value, $"{path}.contains")})",
        "in" => $"{field} in {FormatInList(value, $"{path}.in")}",
        _ => throw new GraphQLArgumentTranslationException($"'{path}' specifies an unknown operator '{operatorName}'.")
    };

    /// <summary>
    /// Formats an <c>in</c> operator's list operand as an OData-esque parenthesized value list.
    /// </summary>
    /// <param name="value">The list operand.</param>
    /// <param name="path">The argument path, used for translation error messages.</param>
    /// <returns>The formatted <c>(value1, value2, ...)</c> expression.</returns>
    private static string FormatInList(object? value, string path)
    {
        if (value is not IEnumerable<object?> list)
            throw new GraphQLArgumentTranslationException($"'{path}' must be a list of values.");

        var items = list.Select(v => FormatValue(v, path)).ToList();
        return items.Count == 0
            ? throw new GraphQLArgumentTranslationException($"'{path}' must specify at least one value.")
            : $"({string.Join(", ", items)})";
    }

    /// <summary>
    /// Formats a scalar operand as its OData-esque literal (quoting/escaping strings, lower-casing booleans, and passing numerics through as-is).
    /// </summary>
    /// <param name="value">The scalar operand.</param>
    /// <param name="path">The argument path, used for translation error messages.</param>
    /// <returns>The formatted OData-esque literal.</returns>
    private static string FormatValue(object? value, string path) => value switch
    {
        null => "null",
        bool b => b ? "true" : "false",
        string s => $"'{s.Replace("'", "''")}'",
        int or long or short or byte or double or float or decimal => Convert.ToString(value, CultureInfo.InvariantCulture)!,
        _ => throw new GraphQLArgumentTranslationException($"'{path}' specifies an unsupported value type '{value.GetType().Name}'.")
    };
}
