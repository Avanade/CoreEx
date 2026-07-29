using CoreEx.Data.GraphQL.Test.Unit.Model;
using CoreEx.Data.Querying;
using CoreEx.RefData;
using CoreEx.RefData.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreEx.Data.GraphQL.Test.Unit;

[TestFixture]
public class GraphQLLiteOptionsTests
{
    private static Task<Person?> GetPersonAsync(GraphQLLiteArgs args, CancellationToken ct) => Task.FromResult<Person?>(new Person { Id = 1 });

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

    [Test]
    public void AddReferenceDataQueries_RegistersEveryType_AliasOrPrimaryName()
    {
        var options = new GraphQLLiteOptions();
        options.AddReferenceDataQueries(CreateOrchestratorServiceProvider(), QueryArgsConfig.Create());

        // The aliased type is exposed under its alias, not its .NET type name.
        options.QueryRoots.Should().ContainKey("ref_widgets_a");
        options.QueryRoots["ref_widgets_a"].ItemType.Should().Be(typeof(WidgetA));
        options.QueryRoots.Should().NotContainKey($"ref_{nameof(WidgetA)}");

        // The non-aliased type is still exposed (bulk-registration is not opt-in-only), keyed by its bare .NET type name.
        options.QueryRoots.Should().ContainKey($"ref_{nameof(WidgetB)}");
        options.QueryRoots[$"ref_{nameof(WidgetB)}"].ItemType.Should().Be(typeof(WidgetB));
    }

    [Test]
    public void AddReferenceDataQueries_ExcludeTypes_OptsTypeOut()
    {
        var options = new GraphQLLiteOptions();
        options.AddReferenceDataQueries(CreateOrchestratorServiceProvider(), QueryArgsConfig.Create(), excludeTypes: [typeof(WidgetB)]);

        options.QueryRoots.Should().ContainKey("ref_widgets_a");
        options.QueryRoots.Should().NotContainKey($"ref_{nameof(WidgetB)}");
    }

    /// <summary>
    /// Builds an <see cref="IServiceProvider"/> exposing a singleton <see cref="ReferenceDataOrchestrator"/> already registered with <see cref="DummyReferenceDataProvider"/>, for use by
    /// <see cref="GraphQLLiteOptions.AddReferenceDataQueries(IServiceProvider, QueryArgsConfig, string?, IEnumerable{Type})"/>.
    /// </summary>
    private static IServiceProvider CreateOrchestratorServiceProvider()
    {
        var providerServices = new ServiceCollection();
        providerServices.AddScoped<DummyReferenceDataProvider>();
        using var providerServiceProvider = providerServices.BuildServiceProvider();

        var orchestrator = new ReferenceDataOrchestrator(providerServiceProvider, NullLogger<ReferenceDataOrchestrator>.Instance);
        orchestrator.Register<DummyReferenceDataProvider>();

        var services = new ServiceCollection();
        services.AddSingleton(orchestrator);
        return services.BuildServiceProvider();
    }

    // Stand-in reference data "types" - only Type identity/name matters for AddReferenceDataQueries registration; no query is ever executed against them in these tests.
    private sealed class WidgetA;
    private sealed class WidgetB;

    /// <summary>
    /// Declares <see cref="WidgetA"/> with the alternate (friendly) name "widgets-a" and <see cref="WidgetB"/> with no alternate name, to prove both are exposed by
    /// <see cref="GraphQLLiteOptions.AddReferenceDataQueries(IServiceProvider, QueryArgsConfig, string?, IEnumerable{Type})"/> - the alias where declared, otherwise the bare .NET type name.
    /// </summary>
    private sealed class DummyReferenceDataProvider : IReferenceDataProvider
    {
        public IEnumerable<(Type, Type)> Types => [(typeof(WidgetA), typeof(WidgetA)), (typeof(WidgetB), typeof(WidgetB))];

        public IEnumerable<(string, Type)>? AlternateNames => [("widgets-a", typeof(WidgetA))];

        public Task<IReferenceDataCollection> GetAsync(Type type, CancellationToken cancellationToken = default) => throw new NotSupportedException("Not required for these tests; only registration/name-mapping behavior is being verified.");
    }
}
