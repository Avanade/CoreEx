using CoreEx.Invokers;

namespace CoreEx.Test.Unit.Invokers;

[TestFixture]
public class InvokerNameAttributeTests
{
    [InvokerName("Custom.Invoker.Name")]
    private class NamedType { }

    private class UnnamedType { }

    [Test]
    public void GetName_WithAttribute_ReturnsAttributeName()
        => InvokerNameAttribute.GetName<NamedType>().Should().Be("Custom.Invoker.Name");

    [Test]
    public void GetName_WithoutAttribute_ReturnsNamespaceFormattedName()
        => InvokerNameAttribute.GetName<UnnamedType>().Should().Be(typeof(UnnamedType).Namespace + "." + nameof(UnnamedType));

    [Test]
    public void GetName_ByType_MatchesGenericOverload()
        => InvokerNameAttribute.GetName(typeof(NamedType)).Should().Be(InvokerNameAttribute.GetName<NamedType>());

    [Test]
    public void GetName_IsCachedAndConsistentAcrossCalls()
    {
        var first = InvokerNameAttribute.GetName<NamedType>();
        var second = InvokerNameAttribute.GetName<NamedType>();
        first.Should().Be(second);
    }

    [Test]
    public void Constructor_NullOrEmptyName_Throws()
    {
        Action act = () => new InvokerNameAttribute(string.Empty);
        act.Should().Throw<ArgumentException>();
    }
}
