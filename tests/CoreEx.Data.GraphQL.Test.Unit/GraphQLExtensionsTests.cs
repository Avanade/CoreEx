using CoreEx.Data.GraphQL.Test.Unit.Model;
using Microsoft.Extensions.DependencyInjection;

namespace CoreEx.Data.GraphQL.Test.Unit;

[TestFixture]
public class GraphQLExtensionsTests
{
    [Test]
    public async Task AddCoreExGraphQLLite_CalledTwice_LastRegistrationWinsAsync()
    {
        var services = new ServiceCollection();

        services.AddCoreExGraphQLLite((options, _) => options.AddGet<Person>("first", (_, _) => Task.FromResult<Person?>(new Person { Id = 1 })!));
        services.AddCoreExGraphQLLite((options, _) => options.AddGet<Person>("second", (_, _) => Task.FromResult<Person?>(new Person { Id = 2 })!));

        using var provider = services.BuildServiceProvider();
        var engine = provider.GetRequiredService<IGraphQLEngine>();

        // The second AddCoreExGraphQLLite call must override the first (plain AddSingleton semantics), not be silently ignored (as TryAddSingleton would do).
        var second = await engine.ExecuteAsync("{ second(id: 2) { id } }");
        second.HasErrors.Should().BeFalse();

        var first = await engine.ExecuteAsync("{ first(id: 1) { id } }");
        first.HasErrors.Should().BeTrue();
    }
}
