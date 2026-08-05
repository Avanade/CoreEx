namespace CoreEx.RefData.Test.Unit;

public class ReferenceDataContextTests
{
    private class TypeA { }
    private class TypeB { }

    [Test]
    public void Date_Default_ReturnsApproximatelyUtcNow()
    {
        var ctx = new ReferenceDataContext();
        var before = DateTimeOffset.UtcNow;
        var date = ctx.Date;
        var after = DateTimeOffset.UtcNow;

        date.Should().NotBeNull();
        date!.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Test]
    public void Date_Default_IsStableOnceRead()
    {
        var ctx = new ReferenceDataContext();
        var first = ctx.Date;
        var second = ctx.Date;
        second.Should().Be(first);
    }

    [Test]
    public void Date_Set_ReturnsSetValue()
    {
        var ctx = new ReferenceDataContext();
        var fixedDate = new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.Zero);
        ctx.Date = fixedDate;
        ctx.Date.Should().Be(fixedDate);
    }

    [Test]
    public void Indexer_NoTypeSpecificDate_ReturnsDate()
    {
        var ctx = new ReferenceDataContext();
        var fixedDate = new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.Zero);
        ctx.Date = fixedDate;

        ctx[typeof(TypeA)].Should().Be(fixedDate);
    }

    [Test]
    public void Indexer_TypeSpecificDateSet_ReturnsThatDate()
    {
        var ctx = new ReferenceDataContext { Date = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero) };
        var typeDate = new DateTimeOffset(2021, 6, 15, 0, 0, 0, TimeSpan.Zero);
        ctx[typeof(TypeA)] = typeDate;

        ctx[typeof(TypeA)].Should().Be(typeDate);
        ctx[typeof(TypeB)].Should().Be(ctx.Date);
    }

    [Test]
    public void Indexer_TypeSpecificDateSetToNull_FallsBackToDate()
    {
        var fixedDate = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var ctx = new ReferenceDataContext { Date = fixedDate };
        ctx[typeof(TypeA)] = new DateTimeOffset(2021, 1, 1, 0, 0, 0, TimeSpan.Zero);

        ctx[typeof(TypeA)] = null;

        ctx[typeof(TypeA)].Should().Be(fixedDate);
    }

    [Test]
    public void Indexer_NullType_Throws()
    {
        var ctx = new ReferenceDataContext();
        Action act = () => _ = ctx[null!];
        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Reset_ClearsDateAndTypeSpecificDates()
    {
        var ctx = new ReferenceDataContext { Date = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero) };
        ctx[typeof(TypeA)] = new DateTimeOffset(2021, 1, 1, 0, 0, 0, TimeSpan.Zero);

        ctx.Reset();

        var before = DateTimeOffset.UtcNow;
        ctx.Date.Should().NotBeNull();
        ctx.Date!.Value.Should().BeOnOrAfter(before.AddSeconds(-2));
        ctx[typeof(TypeA)].Should().Be(ctx.Date);
    }

    [Test]
    public void ImplementsIReferenceDataContext()
    {
        IReferenceDataContext ctx = new ReferenceDataContext();
        ctx.Date = DateTimeOffset.UtcNow;
        ctx.Date.Should().NotBeNull();
    }
}
