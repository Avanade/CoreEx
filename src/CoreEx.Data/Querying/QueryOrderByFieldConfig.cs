﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Data.Querying
{
    /// <summary>
    /// Provides the <see cref="QueryOrderByParser"/> field configuration.
    /// </summary>
    /// <param name="parser">The owning <see cref="QueryOrderByParser"/>.</param>
    /// <param name="field">The field name.</param>
    /// <param name="model">The model name (defaults to <paramref name="field"/>.</param>
    public sealed class QueryOrderByFieldConfig(QueryOrderByParser parser, string field, string? model)
    {
        private readonly string? _model = model;

        /// <summary>
        /// Gets the owning <see cref="QueryFilterParser"/>.
        /// </summary>
        public QueryOrderByParser Parser { get; internal set; } = parser.ThrowIfNull(nameof(parser));

        /// <summary>
        /// Gets the field name.
        /// </summary>
        public string Field { get; } = field.ThrowIfNullOrEmpty(nameof(field));

        /// <summary>
        /// Gets or sets model name to be used for the dynamic LINQ expression.
        /// </summary>
        /// <remarks>Defaults to the <see cref="Field"/> name.</remarks>
        public string? Model => _model ?? Field;

        /// <summary>
        /// Gets the supported <see cref="QueryOrderByDirection"/>.
        /// </summary>
        /// <remarks>Defaults to <see cref="QueryOrderByDirection.Both"/>.</remarks>
        public QueryOrderByDirection Direction { get; private set; } = QueryOrderByDirection.Both;

        /// <summary>
        /// Gets the additional help text.
        /// </summary>
        public string? HelpText { get; private set; }

        /// <summary>
        /// Sets (overrides) the <see cref="Direction"/>.
        /// </summary>
        /// <param name="supportedDirection">The <see cref="QueryOrderByDirection"/>.</param>
        /// <returns>The <see cref="QueryOrderByFieldConfig"/> to support fluent-style method-chaining.</returns>
        /// <remarks>The default is <see cref="QueryOrderByDirection.Both"/>.</remarks>
        public QueryOrderByFieldConfig WithDirection(QueryOrderByDirection supportedDirection)
        {
            Direction = supportedDirection;
            return this;
        }

        /// <summary>
        /// Sets (overrides) the additional help text.
        /// </summary>
        /// <param name="text">The additional help text.</param>
        /// <returns>The <see cref="QueryOrderByFieldConfig"/> to support fluent-style method-chaining.</returns>
        public QueryOrderByFieldConfig WithHelpText(string text)
        {
            HelpText = text.ThrowIfNullOrEmpty(nameof(text));
            return this;
        }
    }
}