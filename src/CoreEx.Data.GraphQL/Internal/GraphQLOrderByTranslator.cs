namespace CoreEx.Data.GraphQL.Internal;

/// <summary>
/// Translates the GraphQL-native <c>orderBy</c> argument (a list of field/direction input objects) into the equivalent OData-esque order-by <see cref="string"/> consumed by
/// <see cref="QueryOrderByParser"/>.
/// </summary>
/// <remarks>The <c>orderBy</c> shape mirrors mainstream GraphQL sorting conventions (e.g. Hot Chocolate, Prisma): a list of input objects, each mapping a field name to a bare
/// <c>ASC</c>/<c>DESC</c> direction token (e.g. <c>orderBy: [{ text: DESC }, { sku: ASC }]</c>), preserving field precedence via list order. <b>Each object should specify a
/// single field</b> — GraphQL input-object field order is not spec-guaranteed, so a multi-key object (e.g. <c>{ text: DESC, sku: ASC }</c>) cannot safely express relative
/// precedence between its keys; use one object per ordered field instead. This is translated 1:1 to the comma-separated <c>field asc|desc, ...</c> string already accepted by
/// <see cref="QueryOrderByParser"/> — the translation is purely syntactic; field legality is enforced by the existing, unmodified parser (defense in depth).</remarks>
internal static class GraphQLOrderByTranslator
{
    /// <summary>
    /// Translates the resolved <c>orderBy</c> argument value to the equivalent OData-esque order-by string.
    /// </summary>
    /// <param name="orderBy">The resolved <c>orderBy</c> argument value (an <see cref="IEnumerable{T}"/> of field/direction objects produced by <see cref="GraphQLValueConverter"/>,
    /// or <see langword="null"/> where not specified).</param>
    /// <returns>The OData-esque order-by string (or <see langword="null"/> where <paramref name="orderBy"/> is <see langword="null"/>).</returns>
    public static string? Translate(object? orderBy)
    {
        if (orderBy is null)
            return null;

        if (orderBy is not IEnumerable<object?> list)
            throw new GraphQLArgumentTranslationException("'orderBy' must be a list of input objects.");

        var clauses = new List<string>();
        var index = 0;

        foreach (var item in list)
        {
            if (item is not IReadOnlyDictionary<string, object?> dict)
                throw new GraphQLArgumentTranslationException($"'orderBy[{index}]' must be an input object.");

            foreach (var (field, direction) in dict)
                clauses.Add(FormatClause(field, direction, $"orderBy[{index}].{field}"));

            index++;
        }

        return clauses.Count == 0 ? throw new GraphQLArgumentTranslationException("'orderBy' must specify at least one field.") : string.Join(", ", clauses);
    }

    /// <summary>
    /// Formats a single field/direction pair as its OData-esque <c>field asc|desc</c> clause.
    /// </summary>
    /// <param name="field">The GraphQL field name.</param>
    /// <param name="direction">The direction token (expected to be <see langword="null"/>, <c>"asc"</c> or <c>"desc"</c>, case-insensitive).</param>
    /// <param name="path">The argument path, used for translation error messages.</param>
    /// <returns>The formatted <c>field</c>, <c>field asc</c> or <c>field desc</c> clause.</returns>
    private static string FormatClause(string field, object? direction, string path)
    {
        GraphQLNameValidator.ValidateFieldName(field, path);

        return direction switch
        {
            null => field,
            string s when string.Equals(s, "asc", StringComparison.OrdinalIgnoreCase) => $"{field} asc",
            string s when string.Equals(s, "desc", StringComparison.OrdinalIgnoreCase) => $"{field} desc",
            _ => throw new GraphQLArgumentTranslationException($"'{path}' direction must be 'ASC' or 'DESC'.")
        };
    }
}
