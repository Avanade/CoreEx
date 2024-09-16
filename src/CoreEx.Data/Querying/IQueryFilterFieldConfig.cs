// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Data.Querying.Expressions;
using CoreEx.Mapping.Converters;
using System;
using System.Text;

namespace CoreEx.Data.Querying
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
        bool IsTypeString { get; }

        /// <summary>
        /// Indicates whether the field type is a <see cref="bool"/>.
        /// </summary>
        bool IsTypeBoolean { get; }

        /// <summary>
        /// Gets the field name.
        /// </summary>
        string Field { get; }

        /// <summary>
        /// Gets or sets model name to be used for the dynamic LINQ expression.
        /// </summary>
        /// <remarks>Defaults to the <see cref="Field"/> name.</remarks>
        string? Model { get; }

        /// <summary>
        /// Gets the supported kinds.
        /// </summary>
        /// <remarks>Where <see cref="IsTypeBoolean"/> defaults to both <see cref="QueryFilterOperator.Equal"/> and <see cref="QueryFilterOperator.NotEqual"/> only; otherwise, defaults to <see cref="QueryFilterOperator.ComparisonOperators"/>.</remarks>
        QueryFilterOperator Operators { get; }

        /// <summary>
        /// Indicates whether the comparison should ignore case or not; will use <see cref="string.ToUpper()"/> when selected for comparisons.
        /// </summary>
        /// <remarks>This is only applicable where the <see cref="IsTypeString"/>.</remarks>
        bool IsToUpper { get; }

        /// <summary>
        /// Indicates whether the field can be <see langword="null"/> or not.
        /// </summary>
        bool IsNullable { get; }

        /// <summary>
        /// Indicates whether a not-<see langword="null"/> check should also be performed before the comparion occurs.
        /// </summary>
        bool IsCheckForNotNull { get; }

        /// <summary>
        /// Gets the default LINQ <see cref="QueryStatement"/> to be used where no filtering is specified.
        /// </summary>
        QueryStatement? DefaultStatement { get; }

        /// <summary>
        /// Gets the additional help text.
        /// </summary>
        string? HelpText { get; }

        /// <summary>
        /// Converts <paramref name="field"/> to the destination type using the <see cref="Converter"/> configurations where specified.
        /// </summary>
        /// <param name="operation">The operation <see cref="QueryFilterTokenKind"/> being performed on the <paramref name="field"/>.</param>
        /// <param name="field">The field <see cref="QueryFilterToken"/>.</param>
        /// <param name="filter">The query filter.</param>
        /// <returns>The converted value.</returns>
        /// <remarks></remarks>
        object? ConvertToValue(QueryFilterToken operation, QueryFilterToken field, string filter);

        /// <summary>
        /// Validate the <paramref name="constant"/> token against the field configuration.
        /// </summary>
        /// <param name="field">The field <see cref="QueryFilterToken"/>.</param>
        /// <param name="constant">The constant <see cref="QueryFilterToken"/>.</param>
        /// <param name="filter">The query filter.</param>
        void ValidateConstant(QueryFilterToken field, QueryFilterToken constant, string filter);

        /// <summary>
        /// Gets the <see cref="QueryFilterFieldResultWriter"/>.
        /// </summary>
        QueryFilterFieldResultWriter? ResultWriter { get; }

        /// <summary>
        /// Appends the field configuration to the <paramref name="stringBuilder"/>.
        /// </summary>
        /// <param name="stringBuilder">The <see cref="StringBuilder"/>.</param>
        /// <returns>The <paramref name="stringBuilder"/>.</returns>
        StringBuilder AppendToString(StringBuilder stringBuilder);
    }
}