using CoreEx.Data;

namespace CoreEx.Test.Unit.Data;

[TestFixture]
public class PagingResultTests
{
    [TearDown]
    public void TearDown()
    {
        PagingArgs.DefaultTake = 25;
    }

    [Test]
    public void Constructor_Default()
    {
        var pr = new PagingResult();
        pr.Skip.Should().Be(0);
        pr.Take.Should().Be(25);
        pr.IsCountRequested.Should().BeFalse();
        pr.TotalCount.Should().BeNull();
    }

    [Test]
    public void Constructor_WithPagingArgs()
    {
        var args = new PagingArgs(3, 7, true);
        var pr = new PagingResult(args);
        pr.Skip.Should().Be(3);
        pr.Take.Should().Be(7);
        pr.IsCountRequested.Should().BeTrue();
    }

    [Test]
    public void TotalCount_SetAndGet()
    {
        var pr = new PagingResult().WithTotalCount(123);
        pr.TotalCount.Should().Be(123);
    }

    [Test]
    public void TotalCount_SetNull()
    {
        var pr = new PagingResult().WithTotalCount(null);
        pr.TotalCount.Should().BeNull();
    }

    [Test]
    public void TotalCount_SetZeroOrNegative()
    {
        var pr = new PagingResult().WithTotalCount(0);
        pr.TotalCount.Should().Be(0);

        pr.WithTotalCount(-5);
        pr.TotalCount.Should().BeNull();
    }

    [Test]
    public void WithTotalCount_Fluent()
    {
        var pr = new PagingResult();
        var ret = pr.WithTotalCount(99);
        ret.Should().BeSameAs(pr);
        pr.TotalCount.Should().Be(99);
    }
}