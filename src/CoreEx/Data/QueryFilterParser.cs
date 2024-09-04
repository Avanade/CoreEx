// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Mapping.Converters;
using System;
using System.Collections.Generic;

namespace CoreEx.Data
{
    /// <summary>
    /// Represents a basic query filter parser with explicitly defined field support.
    /// </summary>
    /// <remarks>Enables basic query filtering with similar syntax to the OData <c><see href="https://docs.oasis-open.org/odata/odata/v4.01/cs01/part2-url-conventions/odata-v4.01-cs01-part2-url-conventions.html#sec_SystemQueryOptionfilter">$filter</see></c>.
    /// Support is limited to the filter tokens as specified by the <see cref="QueryFilterTokenKind"/>.  
    /// <para>This is <b>not</b> intended to be a replacement for OData, GraphQL, etc. but to provide a limited, explicitly supported, dynamic capability to filter an underlying query.</para></remarks>
    public class QueryFilterParser()
    {
        private readonly Dictionary<string, IQueryFilterFieldConfig> _fields = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Adds a <see cref="QueryFilterFieldConfig{T}"/> to the parser for the specified <paramref name="name"/> as-is.
        /// </summary>
        /// <param name="name">The field name used in the query filter specified with the correct casing.</param>
        /// <param name="configure">The optional action enabling further field configuration.</param>
        /// <returns>The <see cref="QueryFilterParser"/> to support fluent-style method-chaining.</returns>
        public QueryFilterParser AddField<T>(string name, Action<QueryFilterFieldConfig<T>>? configure = null) where T : notnull => AddField(name, null, configure);

        /// <summary>
        /// Adds a <see cref="QueryFilterFieldConfig{T}"/> to the parser using the specified <paramref name="overrideName"/> (overrides the <see cref="QueryFilterTokenKind.Field"/> <paramref name="name"/>).
        /// </summary>
        /// <param name="name">The field name used in the query filter.</param>
        /// <param name="overrideName">The field name override.</param>
        /// <param name="configure">The optional action to perform further field configuration.</param>
        /// <returns>The <see cref="QueryFilterParser"/> to support fluent-style method-chaining.</returns>
        /// <remarks>Where the <typeparamref name="T"/> is not a string then will automatically set the <see cref="Converter"/> to <see cref="StringToTypeConverter{T}.Default"/></remarks>
        public QueryFilterParser AddField<T>(string name, string? overrideName, Action<QueryFilterFieldConfig<T>>? configure = null) where T : notnull
        {
            var config = new QueryFilterFieldConfig<T>(this, name, overrideName);
            configure?.Invoke(config);
            _fields.Add(name, config);
            return this;
        }

        /// <summary>
        /// Indicates that at least a single field has been configured.
        /// </summary>
        public bool HasFields => _fields.Count > 0;

        /// <summary>
        /// Gets the <see cref="IQueryFilterFieldConfig"/> for the specified <paramref name="token"/> and automatically throws a <see cref="QueryFilterParserException"/> where not found.
        /// </summary>
        /// <param name="token">The <see cref="QueryFilterToken"/>.</param>
        /// <param name="filter">The query filter.</param>
        /// <returns>The <see cref="IQueryFilterFieldConfig"/>.</returns>
        public IQueryFilterFieldConfig GetFieldConfig(QueryFilterToken token, string filter)
        {
            if (token.Kind != QueryFilterTokenKind.Field)
                throw new ArgumentException($"The token must have a Kind of {QueryFilterTokenKind.Field}.", nameof(token));

            var name = token.GetRawToken(filter).ToString();
            return _fields.TryGetValue(name, out var config) 
                ? config 
                : throw new QueryFilterParserException($"Filter is invalid: {QueryFilterTokenKind.Field} '{name}' is not supported.");
        }

        /// <summary>
        /// Parses and converts the <paramref name="filter"/> to dynamic LINQ.
        /// </summary>
        /// <param name="filter">The query filter.</param>
        /// <returns>The <see cref="QueryFilterParserResult"/>.</returns>
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(filter))]
        public QueryFilterParserResult? Parse(string? filter)
        {
            if (string.IsNullOrEmpty(filter))
                return null;

            var result = new QueryFilterParserResult();

            foreach (var expression in GetExpressions(filter))
            {
                WriteToResult(expression, result);
            }

            return result;
        }

        /// <summary>
        /// Parses and gets the expressions from the <paramref name="filter"/>.
        /// </summary>
        /// <param name="filter">The query filter.</param>
        /// <returns>The <see cref="QueryFilterExpressionBase"/> <see cref="IEnumerable{T}"/>.</returns>
        public IEnumerable<QueryFilterExpressionBase> GetExpressions(string? filter)
        {
            if (!string.IsNullOrEmpty(filter))
            {
                QueryFilterExpressionBase? current = null;
                int expressionCount = 0;
                bool canOpenParen = true;
                bool canLogical = false;
                int parenDepth = 0;

                foreach (var t in GetRawTokens(filter))
                {
                    if (current is not null && !current.CanAddToken(t))
                    {
                        yield return current;
                        expressionCount++;
                        current = null;
                    }

                    if (current is not null)
                        current.AddToken(t);
                    else
                    {
                        if (t.Kind == QueryFilterTokenKind.Not && expressionCount == 0)
                        {
                            current = new QueryFilterLogicalExpression(this, filter, t);
                            canOpenParen = true;
                            canLogical = false;
                        }
                        else if (t.Kind == QueryFilterTokenKind.Field)
                        {
                            current = new QueryFilterOperatorExpression(this, filter, t);
                            canOpenParen = false;
                            canLogical = true;
                        }
                        else if (QueryFilterTokenKind.StringFunction.HasFlag(t.Kind))
                        {
                            current = new QueryFilterStringFunctionExpression(this, filter, t);
                            canOpenParen = false;
                            canLogical = true;
                        }
                        else if (t.Kind == QueryFilterTokenKind.OpenParenthesis)
                        {
                            if (!canOpenParen)
                                throw new QueryFilterParserException($"Filter is invalid: There is a '{t.GetRawToken(filter).ToString()}' positioning that is syntactically incorrect.");

                            current = new QueryFilterOpenParenthesisExpression(this, filter, t);
                            parenDepth++;
                            canLogical = false;
                        }
                        else if (t.Kind == QueryFilterTokenKind.CloseParenthesis)
                        {
                            if (canOpenParen)
                                throw new QueryFilterParserException($"Filter is invalid: There is a '{t.GetRawToken(filter).ToString()}' positioning that is syntactically incorrect.");

                            if (parenDepth == 0)
                                throw new QueryFilterParserException($"Filter is invalid: There is a closing '{t.GetRawToken(filter).ToString()}' that has no matching opening '('.");

                            current = new QueryFilterCloseParenthesisExpression(this, filter, t);
                            parenDepth--;
                            canOpenParen = false;
                            canLogical = true;
                        }
                        else if (QueryFilterTokenKind.Logical.HasFlag(t.Kind))
                        {
                            if (!canLogical)
                                throw new QueryFilterParserException($"Filter is invalid: There is a '{t.GetRawToken(filter).ToString()}' positioning that is syntactically incorrect.");

                            current = new QueryFilterLogicalExpression(this, filter, t);
                            canOpenParen = true;
                            canLogical = false;
                        }
                        else
                            throw new QueryFilterParserException($"Filter is invalid: There is a '{t.GetRawToken(filter).ToString()}' positioning that is syntactically incorrect.");
                    }
                }

                if (current is not null)
                {
                    if (!current.IsComplete)
                        throw new QueryFilterParserException("Filter is invalid: The final expression is incomplete.");

                    yield return current;
                }

                if (parenDepth != 0)
                    throw new QueryFilterParserException("Filter is invalid: There is an opening '(' that has no matching closing ')'.");

                if (!canLogical)
                    throw new QueryFilterParserException("Filter is invalid: The final expression is incomplete.");
            }
        }

        /// <summary>
        /// Parses and gets the raw tokens from the filter with limited validation.
        /// </summary>
        private IEnumerable<QueryFilterToken> GetRawTokens(string filter)
        {
            for (int i = 0; i < filter.Length; i++)
            {
                if (filter[i] == '(')
                {
                    yield return new QueryFilterToken(QueryFilterTokenKind.OpenParenthesis, i, 1);
                    continue;
                }

                if (filter[i] == ')')
                {
                    yield return new QueryFilterToken(QueryFilterTokenKind.CloseParenthesis, i, 1);
                    continue;
                }

                if (filter[i] == ',')
                {
                    yield return new QueryFilterToken(QueryFilterTokenKind.Comma, i, 1);
                    continue;
                }

                if (filter[i] == '\'')
                {
                    var span = filter.AsSpan()[(i + 1)..];
                    var j = FindEndOfLiteral(ref span);
                    if (j == -1)
                        throw new QueryFilterParserException($"Filter is invalid: A {QueryFilterTokenKind.Literal} has not been terminated.");

                    yield return new QueryFilterToken(QueryFilterTokenKind.Literal, i, j + 1);
                    i += j;
                    continue;
                }

                if (filter[i] != ' ')
                {
                    var start = i;
                    var j = i + 1;
                    var backup = false;

                    for (; j < filter.Length; j++)
                    {
                        if (filter[j] == ' ')
                            break;

                        if (filter[j] == '(' || filter[j] == ')' || filter[j] == ',')
                        {
                            backup = true;
                            break;
                        }
                    }

                    var token = filter.AsSpan()[start..j];
                    var kind = QueryFilterTokenKind.Unspecified;

                    // Determine the kind of token where possible.
                    if (token.Length <= 10)
                    {
                        Span<char> lower = new char[token.Length];
                        token.ToLowerInvariant(lower);

                        kind = lower switch
                        {
                            "eq" => QueryFilterTokenKind.Equal,
                            "ne" => QueryFilterTokenKind.NotEqual,
                            "gt" => QueryFilterTokenKind.GreaterThan,
                            "ge" => QueryFilterTokenKind.GreaterThanOrEqual,
                            "lt" => QueryFilterTokenKind.LessThan,
                            "le" => QueryFilterTokenKind.LessThanOrEqual,
                            "in" => QueryFilterTokenKind.In,
                            "true" => QueryFilterTokenKind.True,
                            "false" => QueryFilterTokenKind.False,
                            "null" => QueryFilterTokenKind.Null,
                            "and" => QueryFilterTokenKind.And,
                            "or" => QueryFilterTokenKind.Or,
                            "not" => QueryFilterTokenKind.Not,
                            "startswith" => QueryFilterTokenKind.StartsWith,
                            "endswith" => QueryFilterTokenKind.EndsWith,
                            "contains" => QueryFilterTokenKind.Contains,
                            _ => QueryFilterTokenKind.Unspecified
                        };
                    }

                    if (kind == QueryFilterTokenKind.Unspecified)
                        kind = (token.Length == 32 && Guid.TryParse(token, out _))
                            ? QueryFilterTokenKind.Value 
                            : (char.IsLetter(token[0]) || token[0] == '_') 
                                ? QueryFilterTokenKind.Field 
                                : _fields.ContainsKey(token.ToString())
                                    ? QueryFilterTokenKind.Field
                                    : QueryFilterTokenKind.Value;

                    yield return new QueryFilterToken(kind, start, token.Length);
                    i = backup ? j - 1 : j;
                    continue;
                }
            }
        }

        /// <summary>
        /// Finds the end of a literal.
        /// </summary>
        private static int FindEndOfLiteral(ref ReadOnlySpan<char> filter)
        {
            var inQuote = true;
            var i = 0;
            for (; i < filter.Length; i++)
            {
                if (filter[i] == '\'')
                {
                    if (i < filter.Length - 1)
                    {
                        if (filter[i + 1] == '\'')
                        {
                            i++;
                            continue;
                        }
                    }

                    inQuote = false;
                }
                else if (filter[i] == ' ' || filter[i] == '(' || filter[i] == ')' || filter[i] == ',')
                {
                    if (!inQuote)
                        return i;
                }
            }

            return inQuote ? -1 : i;
        }

        /// <summary>
        /// Converts the query filter <paramref name="expression"/> into the corresponding dynamic LINQ appending to the <paramref name="result"/>.
        /// </summary>
        /// <param name="expression">The <see cref="QueryFilterExpressionBase"/>.</param>
        /// <param name="result">The <see cref="QueryFilterParserResult"/>.</param>
        /// <remarks>Override this method to provide a custom dynamic LINQ conversion.</remarks>
        protected virtual void WriteToResult(QueryFilterExpressionBase expression, QueryFilterParserResult result) => expression.WriteToResult(result);
    }
}