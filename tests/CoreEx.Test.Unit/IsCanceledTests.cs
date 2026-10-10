namespace CoreEx.Test.Unit;

[TestFixture]
public class IsCanceledTests
{
    [Test]
    public void IsCanceled_OperationCanceled() => new OperationCanceledException().IsCanceled().Should().BeTrue();

    [Test]
    public void IsCanceled_TaskCanceled() => new TaskCanceledException().IsCanceled().Should().BeTrue();

    [Test]
    public void IsCanceled_SingleAggregate() => new AggregateException(new TaskCanceledException()).IsCanceled().Should().BeTrue();

    [Test]
    public void IsCanceled_NestedAggregate()
    {
        // Regression: multiple Result.ThrowOnError() boundaries each wrap the error in a new AggregateException.
        new AggregateException(new AggregateException(new TaskCanceledException())).IsCanceled().Should().BeTrue();
    }

    [Test]
    public void IsCanceled_NotCanceled()
    {
        new InvalidOperationException().IsCanceled().Should().BeFalse();
        new AggregateException(new AggregateException(new InvalidOperationException())).IsCanceled().Should().BeFalse();
        new AggregateException().IsCanceled().Should().BeFalse();
    }

    [Test]
    public void IsCanceledBy_MatchingToken()
    {
        using var cts = new CancellationTokenSource();
        new OperationCanceledException(cts.Token).IsCanceledBy(cts.Token).Should().BeTrue();
        new AggregateException(new TaskCanceledException(null, null, cts.Token)).IsCanceledBy(cts.Token).Should().BeTrue();
        new AggregateException(new AggregateException(new OperationCanceledException(cts.Token))).IsCanceledBy(cts.Token).Should().BeTrue();
    }

    [Test]
    public void IsCanceledBy_DifferentToken()
    {
        using var cts = new CancellationTokenSource();
        using var other = new CancellationTokenSource();
        new OperationCanceledException(other.Token).IsCanceledBy(cts.Token).Should().BeFalse();
        new AggregateException(new AggregateException(new OperationCanceledException(other.Token))).IsCanceledBy(cts.Token).Should().BeFalse();
        new InvalidOperationException().IsCanceledBy(cts.Token).Should().BeFalse();
        new AggregateException().IsCanceledBy(cts.Token).Should().BeFalse();
    }
}
