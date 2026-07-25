using CoreEx.Data.GraphQL.Test.Unit.Model;

namespace CoreEx.Data.GraphQL.Test.Unit;

[TestFixture]
public class GraphQLLiteOptionsTests
{
    private static Task<Person?> GetPersonAsync(IReadOnlyDictionary<string, object?> args, CancellationToken ct) => Task.FromResult<Person?>(new Person { Id = 1 });

    [Test]
    public void AddGet_ReservedIntrospectionName_Throws()
    {
        var options = new GraphQLLiteOptions();
        var act = () => options.AddGet<Person>("__schema", GetPersonAsync);
        act.Should().Throw<ArgumentException>().WithMessage("*reserved*");
    }

    [Test]
    public void AddQuery_ReservedIntrospectionName_Throws()
    {
        var options = new GraphQLLiteOptions();
        var act = () => options.AddQuery<Person>("__type", PersonQueryArgsConfig.Default, (qa, pa, ct) => Task.FromResult<IItemsResult<Person>>(new ItemsResult<Person>()));
        act.Should().Throw<ArgumentException>().WithMessage("*reserved*");
    }

    [Test]
    public void AddGet_DuplicateName_Throws()
    {
        var options = new GraphQLLiteOptions();
        options.AddGet<Person>("person", GetPersonAsync);

        var act = () => options.AddGet<Person>("person", GetPersonAsync);
        act.Should().Throw<ArgumentException>().WithMessage("*already registered*");
    }

    [Test]
    public void AddQuery_NameAlreadyUsedByAddGet_Throws()
    {
        var options = new GraphQLLiteOptions();
        options.AddGet<Person>("person", GetPersonAsync);

        var act = () => options.AddQuery<Person>("person", PersonQueryArgsConfig.Default, (qa, pa, ct) => Task.FromResult<IItemsResult<Person>>(new ItemsResult<Person>()));
        act.Should().Throw<ArgumentException>().WithMessage("*already registered*");
    }
}
