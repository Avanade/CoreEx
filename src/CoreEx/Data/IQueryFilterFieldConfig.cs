// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Mapping.Converters;
using System;

namespace CoreEx.Data
{
    /// <summary>
    /// Represents the base <see cref="QueryFilterParser"/> field configuration.
    /// </summary>
    public interface IQueryFilterFieldConfig
    {
        /// <summary>
        /// Gets the owning <see cref="QueryFilterParser"/>.
        /// </summary>
        QueryFilterParser Parser { get; }

        /// <summary>
        /// Gets the field type.
        /// </summary>
        Type Type { get; }

        /// <summary>
        /// Indicates whether the field type is a <see cref="string"/>.
        /// </summary>
        public bool IsTypeString => Type == typeof(string);

        /// <summary>
        /// Indicates whether the field type is a <see cref="bool"/>.
        /// </summary>
        public bool IsTypeBoolean => Type == typeof(bool);

        /// <summary>
        /// Gets the field name.
        /// </summary>
        string Field { get; }

        /// <summary>
        /// Gets or sets the field name override.
        /// </summary>
        string? OverrideName { get; }

        /// <summary>
        /// Gets the name to be used for the dynamic LINQ expression.
        /// </summary>
        public string LinqName => OverrideName ?? Field;

        /// <summary>
        /// Gets the supported kinds.
        /// </summary>
        /// <remarks>Where <see cref="IsTypeBoolean"/> defaults to both <see cref="QueryFilterTokenKind.Equal"/> and <see cref="QueryFilterTokenKind.NotEqual"/>; otherwise, defaults to <see cref="QueryFilterTokenKind.Operator"/>.</remarks>
        QueryFilterTokenKind SupportedKinds { get; }

        /// <summary>
        /// Indicates whether the comparison should ignore case or not (default); will use <see cref="string.ToUpper()"/> when selected for comparisons.
        /// </summary>
        /// <remarks>This is only applicable where the <see cref="IsTypeString"/>.</remarks>
        bool IsIgnoreCase { get; }

        /// <summary>
        /// Indicates whether a not-<see langword="null"/> check should also be performed before the comparion occurs (defaults to <c>false</c>).
        /// </summary>
        bool IsCheckForNotNull { get; }

        /// <summary>
        /// Converts <paramref name="text"/> to the destination type using the <see cref="Converter"/> and <see cref="IsIgnoreCase"/> configurations where specified.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>The converted value.</returns>
        object? ConvertToValue(string text);

        /// <summary>
        /// Validate the <paramref name="constant"/> token against the field configuration.
        /// </summary>
        /// <param name="field">The field <see cref="QueryFilterToken"/>.</param>
        /// <param name="constant">The constant <see cref="QueryFilterToken"/>.</param>
        /// <param name="filter">The query filter.</param>
        void ValidateConstant(QueryFilterToken field, QueryFilterToken constant, string filter);
    }
}