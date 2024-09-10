// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

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
        /// Indicates that a not-<see langword="null"/> check should also be performed before a comparion occurs.
        /// </summary>
        /// <returns>The <typeparamref name="TSelf"/> to support fluent-style method-chaining.</returns>
        /// <remarks>Sets the <see cref="QueryFilterFieldConfigBase.IsCheckForNotNull"/> to <see langword="true"/>.</remarks>
        public TSelf AlsoCheckNotNull()
        {
            IsCheckForNotNull = true;
            return (TSelf)this;
        }

        /// <summary>
        /// Sets (overrides) the default default LINQ statement to be used where no filtering is specified.
        /// </summary>
        /// <param name="statement">The LINQ <see cref="QueryStatement"/>.</param>
        /// <returns></returns>
        /// <remarks>To avoid unnecessary parsing this should be specified as a valid dynamic LINQ statement.
        /// <para>This must be the required expression <b>only</b>. It will be appended as an <i>and</i> to the final LINQ statement.</para></remarks>
        public TSelf Default(QueryStatement? statement)
        {
            DefaultStatement = statement;
            return (TSelf)this;
        }
    }
}