using CoreEx.Events.Publishing;

namespace CoreEx.Events.Test.Unit.Publishing;

[TestFixture]
public class FixedDestinationProviderTests
{
    [Test]
    public void Name_SetAndGet_ShouldReturnValue()
    {
        var provider = new FixedDestinationProvider { Destination = "my-destination" };
        provider.Destination.Should().Be("my-destination");
    }

    [Test]
    public void Name_SetNull_ShouldThrowArgumentNullException()
    {
        Action act = static () => new FixedDestinationProvider { Destination = null! };
        act.Should().Throw<ArgumentNullException>().WithParameterName("value");
    }

    [Test]
    public void Name_SetEmpty_ShouldThrowArgumentException()
    {
        Action act = static () => new FixedDestinationProvider { Destination = "" };
        act.Should().Throw<ArgumentException>().WithParameterName("value");
    }

    [Test]
    public void CreateFrom_EventData_ShouldReturnName()
    {
        var provider = new FixedDestinationProvider { Destination = "fixed-dest" };
        var eventData = new EventData();
        provider.CreateFrom(eventData).Should().Be("fixed-dest");
    }

    [Test]
    public void CreateFrom_String_ShouldReturnName()
    {
        var provider = new FixedDestinationProvider { Destination = "fixed-dest" };
        provider.CreateFrom("any-dest").Should().Be("fixed-dest");
    }

    [Test]
    public void CreateNew_DefaultParams_ShouldReturnName()
    {
        var provider = new FixedDestinationProvider { Destination = "fixed-dest" };
        provider.CreateNew().Should().Be("fixed-dest");
    }

    [Test]
    public void CreateNew_WithParams_ShouldReturnName()
    {
        var provider = new FixedDestinationProvider { Destination = "fixed-dest" };
        provider.CreateNew(MessageType.Command, "domain", true).Should().Be("fixed-dest");
    }
}