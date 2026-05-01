using CoreEx.Data;

namespace CoreEx.Test.Unit.Data;

[TestFixture]
public class PagingArgsTests
{
    [TearDown]
    public void TearDown()
    {
        // Reset static properties to defaults before each test to avoid side effects.
        PagingArgs.DefaultTake = 25;
        PagingArgs.MaximumTake = 1000;
    }

    [Test]
    public void Constructor_Defaults()
    {
        var args = new PagingArgs();
        args.Skip.Should().Be(0);
        args.Take.Should().Be(25);
        args.IsCountRequested.Should().BeFalse();
        args.IsNone.Should().BeFalse();
    }

    [Test]
    public void Constructor_WithValues()
    {
        var args = new PagingArgs(5, 10, true);
        args.Skip.Should().Be(5);
        args.Take.Should().Be(10);
        args.IsCountRequested.Should().BeTrue();
    }

    [Test]
    public void Constructor_Take_Null_UsesDefault()
    {
        var args = new PagingArgs(2, null, false);
        args.Take.Should().Be(25);
    }

    [Test]
    public void Skip_Negative_SetsToZero()
    {
        var args = new PagingArgs(-10, 10, false);
        args.Skip.Should().Be(0);
    }

    [Test]
    public void Take_ZeroOrNegative_SetsToDefaultTake()
    {
        var args1 = new PagingArgs(0, 0, false);
        args1.Take.Should().Be(25);

        var args2 = new PagingArgs(0, -5, false);
        args2.Take.Should().Be(25);
    }

    [Test]
    public void Take_AboveMaximum_SetsToMaximumTake()
    {
        PagingArgs.MaximumTake = 50;
        var args = new PagingArgs(0, 100, false);
        args.Take.Should().Be(50);
    }

    [Test]
    public void Default_StaticProperty()
    {
        var def = PagingArgs.Create();
        def.Skip.Should().Be(0);
        def.Take.Should().Be(25);
        def.IsCountRequested.Should().BeFalse();
        def.IsNone.Should().BeFalse();
    }

    [Test]
    public void None_StaticProperty()
    {
        var none = PagingArgs.None;
        none.Skip.Should().Be(0);
        none.Take.Should().Be(25);
        none.IsCountRequested.Should().BeFalse();
        none.IsNone.Should().BeTrue();
    }

    [Test]
    public void DefaultTake_StaticProperty()
    {
        PagingArgs.DefaultTake = 77;
        var args = new PagingArgs();
        args.Take.Should().Be(77);
    }

    [Test]
    public void MaximumTake_StaticProperty()
    {
        PagingArgs.MaximumTake = 10;
        var args = new PagingArgs(0, 100, false);
        args.Take.Should().Be(10);
    }
}