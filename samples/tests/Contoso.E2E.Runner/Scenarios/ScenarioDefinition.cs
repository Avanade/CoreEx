using Contoso.E2E.Runner.Infrastructure;

namespace Contoso.E2E.Runner.Scenarios;

public sealed class ScenarioDefinition
{
    public required RunnerAttribute Attribute { get; init; }

    public required Func<IScenario> Factory { get; init; }

    public string Name => Attribute.Name;

    public string Text => Attribute.Text;

    public int Order => Attribute.Order;
}