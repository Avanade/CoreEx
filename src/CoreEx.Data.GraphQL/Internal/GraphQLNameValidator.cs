namespace CoreEx.Data.GraphQL.Internal;

/// <summary>
/// Validates that field-name strings sourced from resolved <c>where</c>/<c>orderBy</c> argument dictionaries conform to the GraphQL <c>Name</c> grammar before being composed
/// into an OData-esque filter/order-by <see cref="string"/>.
/// </summary>
/// <remarks>Field names are inherently <c>Name</c>-safe when a document is parsed from GraphQL literal syntax (the parser itself enforces the grammar); however, <c>where</c>/
/// <c>orderBy</c> values supplied via JSON variables (see <see cref="GraphQLValueConverter"/>) are plain JSON object property names with no such guarantee. Without this check,
/// an unvalidated key could be composed directly into the resulting OData-esque string before <see cref="QueryFilterParser"/>/<see cref="QueryOrderByParser"/> ever see it,
/// risking a malformed or injected filter/order-by expression.</remarks>
internal static partial class GraphQLNameValidator
{
    [GeneratedRegex("^[_A-Za-z][_0-9A-Za-z]*$")]
    private static partial Regex NameRegex();

    /// <summary>
    /// Validates that <paramref name="name"/> conforms to the GraphQL <c>Name</c> grammar (<c>/[_A-Za-z][_0-9A-Za-z]*/</c>).
    /// </summary>
    /// <param name="name">The field name to validate.</param>
    /// <param name="path">The GraphQL argument path, used for the translation error message.</param>
    /// <exception cref="GraphQLArgumentTranslationException">Thrown where <paramref name="name"/> does not conform to the GraphQL <c>Name</c> grammar.</exception>
    public static void ValidateFieldName(string name, string path)
    {
        if (!NameRegex().IsMatch(name))
            throw new GraphQLArgumentTranslationException($"'{path}' is not a valid GraphQL field name.");
    }
}
