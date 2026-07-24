using CoreEx.Data.Querying;

namespace CoreEx.Data.GraphQL.Test.Unit.Model;

/// <summary>
/// A minimal <see cref="QueryArgsConfig{TSelf}"/> for <see cref="Person"/> used purely to exercise the GraphQL-lite engine end-to-end.
/// </summary>
internal sealed class PersonQueryArgsConfig : QueryArgsConfig<PersonQueryArgsConfig>
{
    public PersonQueryArgsConfig()
    {
        WithFilter(filter => filter
            .AddField<string>(nameof(Person.Name), c => c.WithOperators(QueryFilterOperator.AllStringOperators))
            .AddField<int>(nameof(Person.Age)));
        WithOrderBy(orderby => orderby
            .AddField(nameof(Person.Name), c => c.WithDefault())
            .AddField(nameof(Person.Age)));
    }
}
