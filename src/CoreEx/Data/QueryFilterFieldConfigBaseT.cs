// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Data
{
    /// <summary>
    /// Provides the base <see cref="QueryFilterParser"/> field configuration extending <see cref="QueryFilterFieldConfigBase"/> with fluent-style method-chaining capabilities.
    /// </summary>
    /// <typeparam name="TSelf">The self <see cref="Type"/> for support fluent-style method-chaining.</typeparam>
    /// <param name="parser">The owning <see cref="QueryFilterParser"/>.</param>
    /// <param name="type">The field type.</param>
    /// <param name="field">The field name.</param>
    /// <param name="overrideName">The field name override.</param>
    public abstract class QueryFilterFieldConfigBase<TSelf>(QueryFilterParser parser, Type type, string field, string? overrideName)
        : QueryFilterFieldConfigBase(parser, type, field, overrideName) where TSelf : QueryFilterFieldConfigBase<TSelf>
    {
        /// <summary>
        /// Sets (overrides) the <see cref="QueryFilterFieldConfigBase.SupportedKinds"/>.
        /// </summary>
        /// <param name="kinds">The supported <see cref="QueryFilterTokenKind"/> flags.</param>
        /// <returns>The <typeparamref name="TSelf"/> to support fluent-style method-chaining.</returns>
        /// <remarks>The default is <see cref="QueryFilterTokenKind.Operator"/>.</remarks>
        public TSelf SupportKinds(QueryFilterTokenKind kinds)
        {
            if (((IQueryFilterFieldConfig)this).IsTypeBoolean)
                throw new NotSupportedException($"{nameof(SupportKinds)} is not supported where {nameof(IQueryFilterFieldConfig.IsTypeBoolean)}.");

            SupportedKinds = kinds;
            return (TSelf)this;
        }

        /// <summary>
        /// Indicates that the operation should ignore case by performing an explicit <see cref="string.ToUpper()"/> comparison.
        /// </summary>
        /// <returns>The <typeparamref name="TSelf"/> to support fluent-style method-chaining.</returns>
        /// <remarks>Sets the <see cref="QueryFilterFieldConfigBase.IsIgnoreCase"/> to <see langword="true"/>.</remarks>
        public TSelf UseUpperCase()
        {
            if (!((IQueryFilterFieldConfig)this).IsTypeString)
                throw new ArgumentException($"A {nameof(UseUpperCase)} can only be specified where the field type is a string.");

            IsIgnoreCase = true;
            return (TSelf)this;
        }

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
    }
}