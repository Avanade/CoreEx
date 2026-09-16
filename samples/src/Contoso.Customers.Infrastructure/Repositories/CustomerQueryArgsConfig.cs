namespace Contoso.Customers.Infrastructure.Repositories;

/// <summary>
/// Provides the <see cref="QueryArgs"/> configuration for <see cref="Contracts.Customer"/>.
/// </summary>
public class CustomerQueryArgsConfig : QueryArgsConfig<CustomerQueryArgsConfig>
{
    public CustomerQueryArgsConfig()
    {
        WithFilter(filter => filter
            .AddField<string>(nameof(Contracts.CustomerBase.FirstName), c => c.WithOperators(QueryFilterOperator.StringFunctions).AsUpperCase())
            .AddField<string>(nameof(Contracts.CustomerBase.LastName), c => c.WithOperators(QueryFilterOperator.StringFunctions).AsUpperCase())
            .AddField<string>(nameof(Contracts.CustomerBase.Email), c => c.WithOperators(QueryFilterOperator.EqualityOperators).AsUpperCase())
            .AddReferenceDataField<Contracts.CustomerType>(nameof(Contracts.CustomerBase.CustomerType), "CustomerTypeCode"));

        WithOrderBy(orderby => orderby
            .AddField(nameof(Contracts.CustomerBase.LastName), c => c.WithDefault().WithAlwaysInclude())
            .AddField(nameof(Contracts.CustomerBase.FirstName)));
    }
}
