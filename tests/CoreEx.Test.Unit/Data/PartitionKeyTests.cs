namespace CoreEx.Data.Test.Unit;

public class PartitionKeyTests
{
    [Test]
    public void GetPartitionId()
    {
        PartitionKey.GetPartitionId("abc", 4).Should().Be(1);
        PartitionKey.GetPartitionId("ABC", 4).Should().Be(1);
        PartitionKey.GetPartitionId("def", 4).Should().Be(2);
        PartitionKey.GetPartitionId("klm", 4).Should().Be(0);
    }

    [Test]
    public void GetPartitionId_DifferentSize()
    { 
        PartitionKey.GetPartitionId("xxx", 4).Should().Be(3);
        PartitionKey.GetPartitionId("xxx", 3).Should().Be(1);
    }

    [Test]
    public void GetPartitionId_CaseSensitive()
    {
        PartitionKey.GetPartitionId("abc", 4, false).Should().Be(2);
        PartitionKey.GetPartitionId("ABC", 4, false).Should().Be(1);
    }

    [Test]
    public void DefaultNoPartitionKey_IsStable()
    {
        PartitionKey.GetPartitionId(PartitionKey.DefaultNoPartitionKey, 4)
            .Should().Be(PartitionKey.GetPartitionId("$none", 4));
    }

    [Test]
    public void ValidatePartitionSize()
    {
        PartitionKey.ValidatePartitionSize(1).Should().Be(1);
        PartitionKey.ValidatePartitionSize(256).Should().Be(256);
        Assert.Throws<ArgumentException>(() => PartitionKey.ValidatePartitionSize(0));
        Assert.Throws<ArgumentException>(() => PartitionKey.ValidatePartitionSize(257));
    }
}
