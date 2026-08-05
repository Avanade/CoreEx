using CoreEx.Database.Outbox;

namespace CoreEx.Database.Test.Unit;

[TestFixture]
public class OutboxDurationTests
{
    [TestCase(0, 1)]      // Below the one-second minimum is clamped up, not down.
    [TestCase(0.4, 1)]    // Rounds away from zero to 0, then clamped to the 1-second minimum.
    [TestCase(0.5, 1)]
    [TestCase(1, 1)]
    [TestCase(2.5, 3)]
    [TestCase(300, 300)]  // Regression: previously Math.Min(300, 1) collapsed every lease to 1 second.
    public void ToSeconds(double totalSeconds, int expected) => OutboxDuration.ToSeconds(TimeSpan.FromSeconds(totalSeconds)).Should().Be(expected);

    [Test]
    public void ToSeconds_NeverReturnsLessThanOne() => OutboxDuration.ToSeconds(TimeSpan.Zero).Should().Be(1);

    [TestCase(1, 2)]     // 1s + max(1, round(0.1)) = 1 + 1 = 2.
    [TestCase(5, 6)]     // 5s + max(1, round(0.5)) = 5 + 1 = 6.
    [TestCase(10, 11)]   // 10s + max(1, round(1.0)) = 10 + 1 = 11.
    [TestCase(300, 330)] // Regression: previously Math.Min(1, round(30)) collapsed the buffer to 1 second (301 total) instead of the intended 10% (330 total).
    public void ToLeaseSecondsWithBuffer(int seconds, int expected) => OutboxDuration.ToLeaseSecondsWithBuffer(TimeSpan.FromSeconds(seconds)).Should().Be(expected);

    [Test]
    public void ToLeaseSecondsWithBuffer_IsAlwaysGreaterThanToSeconds()
    {
        var duration = TimeSpan.FromMinutes(5);
        OutboxDuration.ToLeaseSecondsWithBuffer(duration).Should().BeGreaterThan(OutboxDuration.ToSeconds(duration));
    }
}
