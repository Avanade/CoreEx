using CoreEx.Data;

namespace CoreEx.Test.Unit.Data;

public class PartitionPickerTests
{
    [Test]
    public void GetNextPartitions_ShouldReturnCount_InRange_NoDuplicates()
    {
        // Basic behavior
        var picker = new PartitionPicker(32, 6, 5);
        var parts = picker.GetNextPartitions(DateTimeOffset.UtcNow);

        parts.Should().HaveCount(6);
        parts.Should().OnlyHaveUniqueItems();
        parts.Should().OnlyContain(p => p >= 0 && p < 32);
    }

    [Test]
    public void GetNextPartitions_All_ShouldReturnAll_WithLastSuccessFirst()
    {
        // Fast path: all partitions
        var picker = new PartitionPicker(8, 8, 5);
        picker.PrioritizePartition(3);

        var parts = picker.GetNextPartitions(DateTimeOffset.UtcNow);

        parts.Should().HaveCount(8);
        parts[0].Should().Be(3);
        parts.Should().OnlyHaveUniqueItems();
        parts.OrderBy(x => x).Should().Equal(Enumerable.Range(0, 8));
    }

    [Test]
    public void GetNextPartitions_Single_ShouldPreferLastSuccess_ElseDeterministicStart()
    {
        // Fast path: single partition
        var picker = new PartitionPicker(16, 1, 5);
        picker.PrioritizePartition(7);
        picker.GetNextPartitions(DateTimeOffset.UtcNow)
              .Should().Equal([7]);

        var picker2 = new PartitionPicker(16, 1, 5);
        var parts2 = picker2.GetNextPartitions(DateTimeOffset.UtcNow);
        parts2.Should().HaveCount(1);
        parts2[0].Should().BeInRange(0, 15);
    }

    [Test]
    public void ReportSuccess_ShouldBiasNextPick_FirstItemIsLastSuccess()
    {
        // Temporal locality
        var picker = new PartitionPicker(32, 6, 5);
        picker.PrioritizePartition(12);

        var parts = picker.GetNextPartitions(DateTimeOffset.UtcNow);
        parts[0].Should().Be(12);
    }

    [Test]
    public void GetNextPartitions_ShouldChangeAcrossEpochs()
    {
        // Epoch rotation changes selection
        var picker = new PartitionPicker(32, 6, 5);
        var t1 = DateTimeOffset.FromUnixTimeSeconds(10_000);
        var t2 = DateTimeOffset.FromUnixTimeSeconds(10_000 + 5);

        var a = picker.GetNextPartitions(t1);
        var b = picker.GetNextPartitions(t2);

        a.Should().NotEqual(b);
    }

    [Test]
    public void TwoPickers_ShouldHaveLowIntersection()
    {
        // Collision reduction heuristic
        var a = new PartitionPicker(32, 6, 5);
        var b = new PartitionPicker(32, 6, 5);
        var now = DateTimeOffset.UtcNow;

        var pa = a.GetNextPartitions(now);
        var pb = b.GetNextPartitions(now);

        pa.Intersect(pb).Count().Should().BeLessThanOrEqualTo(3);
    }

    [TestCase(32, 10)]
    [TestCase(30, 10)]
    public void GetNextPartitions_ShouldHaveUniqueWindow(int total, int window)
    {
        // Coprime stride distribution (no repeats in window)
        var picker = new PartitionPicker(total, window, 5);
        var parts = picker.GetNextPartitions(DateTimeOffset.UtcNow);

        parts.Should().HaveCount(window);
        parts.Should().OnlyHaveUniqueItems();
    }
}