namespace CoreEx.Data.GraphQL.Internal;

/// <summary>
/// Represents an error raised while translating a GraphQL-native structured argument (<c>where</c>, <c>orderBy</c>, <c>first</c>/<c>after</c>) into its underlying
/// <see cref="Querying.QueryArgsConfig"/>-compatible representation.
/// </summary>
/// <remarks>This is distinct from <see cref="Querying.QueryFilterParserException"/>/<see cref="Querying.QueryOrderByParserException"/>, which are raised by the underlying parser
/// <i>after</i> translation has produced a syntactically well-formed OData-esque string; this exception is raised where the GraphQL argument's own shape is invalid (e.g. an
/// unknown operator key, or a list expected where a scalar was supplied) and translation cannot proceed.</remarks>
/// <param name="message">The error message.</param>
internal sealed class GraphQLArgumentTranslationException(string message) : Exception(message)
{
}
