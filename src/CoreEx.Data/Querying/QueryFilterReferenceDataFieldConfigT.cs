namespace CoreEx.Data.Querying;

/// <summary>
/// Provides the <see cref="QueryFilterParser"/> <see cref="IReferenceData"/> field configuration.
/// </summary>
/// <typeparam name="TRef">The <see cref="IReferenceData"/> <see cref="Type"/>.</typeparam>
/// <remarks>Defaults the <see cref="QueryFilterFieldConfigBase.Operators"/> to <see cref="QueryFilterOperator.EqualityOperators"/> only.</remarks>
public class QueryFilterReferenceDataFieldConfig<TRef> : QueryFilterFieldConfigBase<QueryFilterReferenceDataFieldConfig<TRef>> where TRef : IReferenceData, new()
{
    private bool _mustBeActive;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryFilterReferenceDataFieldConfig{TRef}"/> class.
    /// </summary>
    /// <param name="parser">The owning <see cref="QueryFilterParser"/>.</param>
    /// <param name="field">The field name.</param>
    /// <param name="model">The model name (defaults to <paramref name="field"/>).</param>
    public QueryFilterReferenceDataFieldConfig(QueryFilterParser parser, string field, string? model) : base(parser, typeof(TRef), field, model)
    {
        Operators = QueryFilterOperator.EqualityOperators;
        FieldType = QueryFilterFieldType.String;
    }

    /// <summary>
    /// Indicates that the resulting converted value must be <see cref="IReferenceData.IsActive"/>.
    /// </summary>
    /// <param name="mustBeActive"><see langword="true"/> indicates that an error will occur where not active; otherwise, <see langword="false"/>.</param>
    /// <returns>The <see cref="QueryFilterReferenceDataFieldConfig{T}"/> to support fluent-style method-chaining.</returns>
    /// <remarks>Defaults to <see langword="true"/>.</remarks>
    public QueryFilterReferenceDataFieldConfig<TRef> MustBeActive(bool mustBeActive = true)
    {
        _mustBeActive = mustBeActive;
        return this;
    }

    /// <inheritdoc/>
    protected override object ConvertToValue(QueryFilterToken operation, QueryFilterToken field, string filter)
    {
        var code = field.GetValueToken(filter);
        if (!ReferenceDataOrchestrator.TryGetByCode<TRef>(code, out var rd) || !rd.IsValid)
            throw new FormatException($"Not a valid {typeof(TRef).Name}.");

        if (!rd.IsActive && _mustBeActive)
            throw new FormatException($"Not an active {typeof(TRef).Name}.");

        return rd.Code!;
    }
}