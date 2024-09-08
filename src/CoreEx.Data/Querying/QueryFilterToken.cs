// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Data.Querying
{
    /// <summary>
    /// Represents a <see cref="QueryFilterParser"/> token.
    /// </summary>
    /// <param name="kind">The token kind.</param>
    /// <param name="index">The token index.</param>
    /// <param name="length">The token length.</param>
    public readonly struct QueryFilterToken(QueryFilterTokenKind kind, int index, int length)
    {
        /// <summary>
        /// Gets an unspecified <see cref="QueryFilterToken"/>.
        /// </summary>
        public static QueryFilterToken Unspecified { get; } = new QueryFilterToken(QueryFilterTokenKind.Unspecified, 0, 0);

        /// <summary>
        /// Gets the token kind.
        /// </summary>
        public QueryFilterTokenKind Kind { get; } = kind;

        /// <summary>
        /// Gets the token start position.
        /// </summary>
        public int Index { get; } = index;

        /// <summary>
        /// Gets the token length.
        /// </summary>
        public int Length { get; } = length;

        /// <summary>
        /// Gets the raw token from the <paramref name="filter"/>.
        /// </summary>
        /// <param name="filter">The query filter.</param>
        /// <returns>The raw token.</returns>
        public readonly ReadOnlySpan<char> GetRawToken(string filter) => filter.ThrowIfNull(nameof(filter)).AsSpan(Index, Length);

        /// <summary>
        /// Gets the value from the token removing leading and trailing quotes, and replacing all escaped quotes where applicable.
        /// </summary>
        /// <param name="filter">The query filter.</param>
        /// <returns>The value token.</returns>
        public readonly string GetValueToken(string filter)
        {
            var raw = GetRawToken(filter);
            return raw.Length >= 2 && raw[0] == '\'' && raw[^1] == '\'' ? raw[1..^1].ToString().Replace("''", "'") : raw.ToString();
        }

        /// <summary>
        /// Performs a <see cref="GetValueToken"/> and converts using the <paramref name="config">configured</paramref> <see cref="QueryFilterFieldConfigBase.ConvertToValue(QueryFilterToken, QueryFilterToken, string)"/>.
        /// </summary>
        /// <param name="operation">The operation <see cref="QueryFilterToken"/> being performed on the <paramref name="field"/>.</param>
        /// <param name="field">The field <see cref="QueryFilterToken"/>.</param>
        /// <param name="config">The <see cref="QueryFilterFieldConfigBase"/>.</param>
        /// <param name="filter">The query filter.</param>
        /// <returns>The converted value.</returns>
        public readonly object GetConvertedValue(QueryFilterToken operation, QueryFilterToken field, IQueryFilterFieldConfig config, string filter)
        {
            if (Kind != QueryFilterTokenKind.Value && Kind != QueryFilterTokenKind.Literal)
                throw new InvalidOperationException($"A {nameof(GetConvertedValue)} for a token with a {nameof(Kind)} of '{Kind}' is not supported.");

            try
            {
                return config.ConvertToValue(operation, this, filter) ?? throw new InvalidOperationException($"Field '{field.GetRawToken(filter).ToString()}' has a value '{GetValueToken(filter)}' which has been converted to null.");
            }
            catch (QueryFilterParserException)
            {
                throw;
            }
            catch (Exception ex) when (ex is FormatException || ex is InvalidCastException || ex is ValidationException)
            {
                throw new QueryFilterParserException($"Field '{field.GetRawToken(filter).ToString()}' with value '{GetValueToken(filter)}' is invalid: {ex.Message}");
            }
            catch (Exception)
            {
                throw new QueryFilterParserException($"Field '{field.GetRawToken(filter).ToString()}' has a value '{GetValueToken(filter)}' that is not a valid {config.Type.Name}.");
            }
        }

        /// <summary>
        /// Clones and updates the token with the specified <paramref name="kind"/>.
        /// </summary>
        /// <param name="kind">The overridding <see cref="QueryFilterTokenKind"/>.</param>
        /// <returns>The new <see cref="QueryFilterToken"/>.</returns>
        public readonly QueryFilterToken CloneAs(QueryFilterTokenKind kind) => new(kind, Index, Length);

        /// <summary>
        /// Converts the token to the dynamic LINQ equivalent.
        /// </summary>
        /// <param name="filter">The originating filter.</param>
        /// <returns>The dynamic LINQ expression.</returns>
        public readonly string ToLinq(string filter) => Kind switch
        {
            QueryFilterTokenKind.Field => GetRawToken(filter).ToString(),
            QueryFilterTokenKind.True => "true",
            QueryFilterTokenKind.False => "false",
            QueryFilterTokenKind.Null => "null",
            QueryFilterTokenKind.And => "&&",
            QueryFilterTokenKind.Or => "||",
            QueryFilterTokenKind.Not => "!",
            QueryFilterTokenKind.Equal => "==",
            QueryFilterTokenKind.NotEqual => "!=",
            QueryFilterTokenKind.GreaterThan => ">",
            QueryFilterTokenKind.GreaterThanOrEqual => ">=",
            QueryFilterTokenKind.LessThan => "<",
            QueryFilterTokenKind.LessThanOrEqual => "<=",
            QueryFilterTokenKind.In => "in",
            QueryFilterTokenKind.OpenParenthesis => "(",
            QueryFilterTokenKind.CloseParenthesis => ")",
            QueryFilterTokenKind.StartsWith => nameof(QueryFilterTokenKind.StartsWith),
            QueryFilterTokenKind.EndsWith => nameof(QueryFilterTokenKind.EndsWith),
            QueryFilterTokenKind.Contains => nameof(QueryFilterTokenKind.Contains),
            _ => throw new InvalidOperationException($"A {nameof(ToLinq)} for a token with a {nameof(Kind)} of '{Kind}' is not supported."),
        };
    }
}