// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Data.Querying
{
    /// <summary>
    /// Provides the <see cref="QueryFilterParser"/> <i>null only</i> comparison field configuration.
    /// </summary>
    public class QueryFilterNullFieldConfig : QueryFilterFieldConfigBase<QueryFilterNullFieldConfig>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueryFilterNullFieldConfig"/> class.
        /// </summary>
        /// <param name="parser">The owning <see cref="QueryFilterParser"/>.</param>
        /// <param name="field">The field name.</param>
        /// <param name="model">The model name (defaults to <paramref name="field"/>.</param>
        public QueryFilterNullFieldConfig(QueryFilterParser parser, string field, string? model) : base(parser, typeof(object), field, model) => SupportedKinds = QueryFilterTokenKind.Equal | QueryFilterTokenKind.NotEqual;

        /// <inheritdoc/>
        protected override object ConvertToValue(QueryFilterToken operation, QueryFilterToken field, string filter)
            => throw new ValidationException("Only null comparisons are supported.");
    }
}