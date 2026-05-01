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
}