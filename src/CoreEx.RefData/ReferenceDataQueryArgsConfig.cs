namespace CoreEx.RefData;

/// <summary>
/// Provides the ODATA-esque dynamic LINQ queries execution configuration for <see cref="IReferenceData"/> entries with a <see cref="Default"/> instance.
/// </summary>
public sealed class ReferenceDataQueryArgsConfig : QueryArgsConfig
{
    /// <summary>
    /// Gets the default <see cref="ReferenceDataQueryArgsConfig"/> instance.
    /// </summary>
    public static ReferenceDataQueryArgsConfig Default { get; } = new ReferenceDataQueryArgsConfig();

    /// <summary>
    /// Initializes a new instance of the <see cref="ReferenceDataQueryArgsConfig"/> class.
    /// </summary>
    public ReferenceDataQueryArgsConfig()
    {
        WithFilter(f => f
            .AddField<string>(nameof(IReferenceData.Code), c => c.WithOperators(QueryFilterOperator.AllStringOperators).AsUpperCase())
            .AddField<string>(nameof(IReferenceData.Text), c => c.WithOperators(QueryFilterOperator.AllStringOperators).AsUpperCase()));

        WithOrderBy(o => o
            .AddField(nameof(IReferenceData.Code))
            .AddField(nameof(IReferenceData.Text))
            .AddField(nameof(IReferenceData.SortOrder)));
    }
}
