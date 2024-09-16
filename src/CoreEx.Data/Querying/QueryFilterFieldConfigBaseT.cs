// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Data.Querying.Expressions;
using System;

namespace CoreEx.Data.Querying
{
    /// <summary>
    /// Provides the base <see cref="QueryFilterParser"/> field configuration extending <see cref="QueryFilterFieldConfigBase"/> with fluent-style method-chaining capabilities.
    /// </summary>
    /// <typeparam name="TSelf">The self <see cref="Type"/> for support fluent-style method-chaining.</typeparam>
    /// <param name="parser">The owning <see cref="QueryFilterParser"/>.</param>
    /// <param name="type">The field type.</param>
    /// <param name="field">The field name.</param>
    /// <param name="model">The model name (defaults to <paramref name="field"/>.</param>
    public abstract class QueryFilterFieldConfigBase<TSelf>(QueryFilterParser parser, Type type, string field, string? model)
        : QueryFilterFieldConfigBase(parser, type, field, model) where TSelf : QueryFilterFieldConfigBase<TSelf>
    {
        /// <summary>
        /// Indicates that the field can be <see langword="null"/>.
        /// </summary>
        /// <returns>The <typeparamref name="TSelf"/> to support fluent-style method-chaining.</returns>
        /// <remarks>Sets the <see cref="QueryFilterFieldConfigBase.IsNullable"/> to <see langword="true"/>.</remarks>
        public TSelf AsNullable()
        {
            IsNullable = true;
            return (TSelf)this;
        }

        /// <summary>
        /// Indicates that a not-<see langword="null"/> check should also be performed before a comparion occurs.
        /// </summary>
        /// <returns>The <typeparamref name="TSelf"/> to support fluent-style method-chaining.</returns>
        /// <remarks>Sets the <see cref="QueryFilterFieldConfigBase.IsCheckForNotNull"/> and <see cref="QueryFilterFieldConfigBase.IsNullable"/> to <see langword="true"/>.</remarks>
        public TSelf AlsoCheckNotNull()
        {
            IsCheckForNotNull = true;
            IsNullable = true;
            return (TSelf)this;
        }

        /// <summary>
        /// Sets (overrides) the default default LINQ statement to be used where no filtering is specified.
        /// </summary>
        /// <param name="statement">The LINQ <see cref="QueryStatement"/>.</param>
        /// <returns>The <typeparamref name="TSelf"/> to support fluent-style method-chaining.</returns>
        /// <remarks>To avoid unnecessary parsing this should be specified as a valid dynamic LINQ statement.
        /// <para>This must be the required expression <b>only</b>. It will be appended as an <i>and</i> to the final LINQ statement.</para></remarks>
        public TSelf WithDefault(QueryStatement? statement)
        {
            DefaultStatement = statement;
            return (TSelf)this;
        }

        /// <summary>
        /// Sets (overrides) the function that will be used to write the <see cref="IQueryFilterFieldStatementExpression"/> LINQ statement to the <see cref="QueryFilterParserResult.FilterBuilder"/>.
        /// </summary>
        /// <param name="resultWriter">The <see cref="QueryFilterFieldResultWriter"/>.</param>
        /// <returns>The <typeparamref name="TSelf"/> to support fluent-style method-chaining.</returns>
        public TSelf WithResultWriter(QueryFilterFieldResultWriter? resultWriter)
        {
            ResultWriter = resultWriter;
            return (TSelf)this;
        }

        /// <summary>
        /// Sets (overrides) the additional help text.
        /// </summary>
        /// <param name="text">The additional help text.</param>
        /// <returns>The <typeparamref name="TSelf"/> to support fluent-style method-chaining.</returns>
        public TSelf WithHelpText(string text)
        {
            HelpText = text.ThrowIfNullOrEmpty(nameof(text));
            return (TSelf)this;
        }
    }
}