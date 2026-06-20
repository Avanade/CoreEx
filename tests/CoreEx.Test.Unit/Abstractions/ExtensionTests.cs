namespace CoreEx.Test.Unit.Abstractions;

public class ExtenstionTests
{
    [Test]
    public void ThrowIfNull_WhenNull()
    {
        string aussie = null!;
        var act = () => aussie.ThrowIfNull();
        act.Should().Throw<ArgumentNullException>().WithParameterName("aussie");
    }

    [Test]
    public void ThrowIfNull_WhenNotNull()
    {
        string aussie = "Aussie";
        aussie.ThrowIfNull().Should().Be("Aussie");
    }

    [Test]
    public void Adjust_Value_NonNullable()
    {
        var p = new Person();
        var p2 = p.Adjust(x => x.Name = "Babs");
        p.Name.Should().Be("Babs");
        p2.Name.Should().Be("Babs");
    }

    [Test]
    public void Adjust_Value_Nullable()
    {
        Person? p = null;
        p.Adjust(x => x.Name = "Babs");
        p.Should().BeNull();
    }

    [Test]
    public void Adjust_Value_Nullable_With_Value()
    {
        Person? p = new();
        var p2 = p.Adjust(x => x.Name = "Babs");
        p.Should().NotBeNull();
        p.Name.Should().Be("Babs");
        p2.Name.Should().Be("Babs");
    }

    public class Person { public string? Name { get; set; } }
}