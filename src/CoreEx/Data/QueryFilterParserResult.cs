// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;
using System.Text;

namespace CoreEx.Data
{
    /// <summary>
    /// Represents the result of a successful <see cref="QueryFilterParser.Parse(string?)"/>.
    /// </summary>
    public class QueryFilterParserResult
    {
        /// <summary>
        /// Gets the resulting dynamic LINQ filter <see cref="StringBuilder"/>.
        /// </summary>
        public StringBuilder FilterBuilder { get; } = new StringBuilder();

        /// <summary>
        /// Appends a value to the <see cref="FilterBuilder"/> as an argument.
        /// </summary>
        /// <param name="value">The value.</param>
        public void AppendValue(object? value)
        {
            Args.Add(value);
            FilterBuilder.Append($"@{Args.Count - 1}");
        }

        /// <summary>
        /// Appends a space to the <see cref="FilterBuilder"/> if required.
        /// </summary>
        public void AppendSpaceIfRequired()
        {
            if (FilterBuilder.Length > 0 && FilterBuilder[^1] != ' ' && FilterBuilder[^1] != '!')
                FilterBuilder.Append(' ');
        }

        /// <summary>
        /// Appends a <paramref name="char"/> to the <see cref="FilterBuilder"/>.
        /// </summary>
        /// <param name="char">The chararater to append.</param>
        public void Append(char @char)
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
        /// <param name="span"></param>
        public void Append(ReadOnlySpan<char> span)
        {
            if (FilterBuilder.Length > 0 && FilterBuilder[^1] != ' ' && FilterBuilder[^1] != '!' && FilterBuilder[^1] != '(')
                FilterBuilder.Append(' ');

            FilterBuilder.Append(span);
        }

        /// <summary>
        /// Gets the resulting arguments referenced by the <see cref="FilterBuilder"/>.
        /// </summary>
        public List<object?> Args { get; } = [];
    }
}