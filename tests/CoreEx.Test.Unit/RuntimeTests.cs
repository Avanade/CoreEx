namespace CoreEx.Test.Unit;

[TestFixture]
public class RuntimeTests
{
    [TearDown]
    public void TearDown() => ExecutionContext.Reset();

    [Test]
    public void UtcNow_NoCurrentExecutionContext_ReturnsSystemTime()
    {
        ExecutionContext.HasCurrent.Should().BeFalse();

        var before = DateTimeOffset.UtcNow;
        var result = global::CoreEx.Runtime.UtcNow;
        var after = DateTimeOffset.UtcNow;

        result.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Test]
    public void UtcNow_WithCurrentExecutionContext_ReturnsItsTimestamp()
    {
        var fixedTime = new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var ec = new ExecutionContext { Timestamp = fixedTime };
        ExecutionContext.SetCurrent(ec);

        global::CoreEx.Runtime.UtcNow.Should().Be(fixedTime);
    }

    [Test]
    public void NewGuid_ReturnsNonEmptyGuid()
    {
        var guid = global::CoreEx.Runtime.NewGuid();
        guid.Should().NotBe(Guid.Empty);
    }

    [Test]
    public void NewGuid_ReturnsDistinctValues()
    {
        var g1 = global::CoreEx.Runtime.NewGuid();
        var g2 = global::CoreEx.Runtime.NewGuid();
        g1.Should().NotBe(g2);
    }

    [Test]
    public void NewId_ReturnsGuidFormattedString()
    {
        var id = global::CoreEx.Runtime.NewId();
        Guid.TryParse(id, out _).Should().BeTrue();
    }
}
