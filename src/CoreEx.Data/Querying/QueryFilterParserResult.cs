// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;
using System.Text;

namespace CoreEx.Data.Querying
{
    /// <summary>
    /// Represents the result of a <see cref="QueryFilterParser.Parse(string?)"/>.
    /// </summary>
    public sealed class QueryFilterParserResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueryFilterParserResult"/> that is a success.
        /// </summary>
        internal QueryFilterParserResult() { }

        /// <summary>
        /// Gets the field names referenced within the resulting LINQ query.
        /// </summary>
        public HashSet<string> Fields { get; } = [];

        /// <summary>
        /// Gets the resulting dynamic LINQ filter <see cref="StringBuilder"/>.
        /// </summary>
        internal StringBuilder FilterBuilder { get; } = new StringBuilder();

        /// <summary>
        /// Gets the resulting arguments referenced by the <see cref="FilterBuilder"/>.
        /// </summary>
        public List<object?> Args { get; } = [];

        /// <summary>
        /// Provides the resulting dynamic LINQ filter.
        /// </summary>
        public string? ToLinqString() => FilterBuilder.ToString();

        /// <summary>
        /// Appends a value to the <see cref="FilterBuilder"/> as a placeholder and adds into the <see cref="Args"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        public void AppendValue(object? value)
        {
            Args.Add(value);
            FilterBuilder.Append($"@{Args.Count - 1}");
        }

        /// <summary>
        /// Appends a <paramref name="char"/> to the <see cref="FilterBuilder"/>.
        /// </summary>
        /// <param name="char">The chararater to append.</param>
        /// <remarks>Also appends a space if required.</remarks>
        internal void Append(char @char)
        {
            if (FilterBuilder.Length > 0 && FilterBuilder[^1] != ' ' && FilterBuilder[^1] != '!' && FilterBuilder[^1] != '(')
            {
                if (!(@char == ')' && FilterBuilder[^1] == ')'))
                    FilterBuilder.Append(' ');
            }

            FilterBuilder.Append(@char);
        }

        /// <summary>
        /// Appends a <paramref name="span"/> to the <see cref="FilterBuilder"/>.
        /// </summary>
        /// <param name="span">The span.</param>
        /// <remarks>Also appends a space if required.</remarks>
        internal void Append(ReadOnlySpan<char> span)
        {
            if (FilterBuilder.Length > 0 && FilterBuilder[^1] != ' ' && FilterBuilder[^1] != '!' && FilterBuilder[^1] != '(')
                FilterBuilder.Append(' ');

            FilterBuilder.Append(span);
        }

        /// <summary>
        /// Appends a <paramref name="statement"/> to the <see cref="FilterBuilder"/>.
        /// </summary>
        /// <param name="statement">The <see cref="QueryStatement"/>.</param>
        /// <remarks>Also appends an '<c> &amp;&amp; </c>' (and) prior to the <paramref name="statement"/> where neccessary.</remarks>
        public void AppendStatement(QueryStatement statement)
        {
            statement.ThrowIfNull(nameof(statement));
            if (FilterBuilder.Length > 0)
                FilterBuilder.Append(" && ");

            var sb = new StringBuilder(statement.Statement);
            for (int i = 0; i < statement.Args.Length; i++)
            {
                sb.Replace($"@{i}", $"@{Args.Count}");
                Args.Add(statement.Args[i]);
            }

            FilterBuilder.Append(sb);
        }

        /// <summary>
        /// Defaults the <see cref="FilterBuilder"/> with the specified <paramref name="statement"/> where not already set.
        /// </summary>
        /// <param name="statement">The <see cref="QueryStatement"/>.</param>
        public void UseDefault(QueryStatement? statement) => UseDefault(statement is null ? null : () => statement);

        /// <summary>
        /// Defaults the <see cref="FilterBuilder"/> with the specified <paramref name="statement"/> function where not already set.
        /// </summary>
        /// <param name="statement">The <see cref="QueryStatement"/> function.</param>
        public void UseDefault(Func<QueryStatement>? statement)
        {
            if (FilterBuilder.Length > 0)
                return;

            var stmt = statement?.Invoke();
            if (stmt is not null)
            {
                FilterBuilder.Append(stmt.Statement);
                Args.AddRange(stmt.Args);
            }
        }
    }
}