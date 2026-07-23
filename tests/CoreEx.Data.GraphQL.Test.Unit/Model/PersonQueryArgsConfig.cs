using CoreEx.Data.Querying;

namespace CoreEx.Data.GraphQL.Test.Unit.Model;

/// <summary>
/// A minimal <see cref="QueryArgsConfig{TSelf}"/> for <see cref="Person"/> used purely to exercise the GraphQL-lite engine end-to-end.
/// </summary>
internal sealed class PersonQueryArgsConfig : QueryArgsConfig<PersonQueryArgsConfig>
{
    public PersonQueryArgsConfig()
    {
        WithFilter(filter => filter.AddField<string>(nameof(Person.Name)));
        WithOrderBy(orderby => orderby.AddField(nameof(Person.Name), c => c.WithDefault()));
    }
}
