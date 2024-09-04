// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Data
{
    /// <summary>
    /// Provides the <see cref="QueryOrderByParser"/> field configuration.
    /// </summary>
    /// <param name="parser">The owning <see cref="QueryOrderByParser"/>.</param>
    /// <param name="field">The field name.</param>
    public class QueryOrderByFieldConfig(QueryOrderByParser parser, string field)
    {
        /// <summary>
        /// Gets the owning <see cref="QueryFilterParser"/>.
        /// </summary>
        public QueryOrderByParser Parser { get; internal set; } = parser.ThrowIfNull(nameof(parser));

        /// <summary>
        /// Gets the field name.
        /// </summary>
        public string Field { get; } = field;

        /// <summary>
        /// Gets or sets the field name override.
        /// </summary>
        public string? OverrideField { get; set; }

        /// <summary>
        /// Indicates whether an <i>ascending</i> sort is supported for the field.
        /// </summary>
        public bool SupportsAscending { get; set; } = true;

        /// <summary>
        /// Indicates whether a <i>descending</i> sort is supported for the field.
        /// </summary>
        public bool SupportsDescending { get; set; } = true;
    }
}