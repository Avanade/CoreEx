namespace CoreEx.Test.Unit.Hosting.Synchronization;

[TestFixture]
public class CoordinationLeaseTests
{
    private static CoreEx.Coordination.CoordinationOptions Options => new()
    {
        LeaseDuration = TimeSpan.FromMilliseconds(700),
        RenewalInterval = TimeSpan.FromMilliseconds(100),
        OperationTimeout = TimeSpan.FromMilliseconds(150)
    };

    [Test]
    public async Task RefusedRenewal_LosesOwnershipIrreversibly()
    {
        await using var lease = new CoreEx.Coordination.CoordinationLease(Options, System.Diagnostics.Stopwatch.GetTimestamp(), _ => Task.FromResult(false), _ => Task.CompletedTask, NullLogger.Instance);
        Func<Task> wait = () => Task.Delay(Timeout.InfiniteTimeSpan, lease.OwnershipLost).WaitAsync(TimeSpan.FromSeconds(3));
        await wait.Should().ThrowAsync<OperationCanceledException>();
        Action ensure = lease.EnsureOwned;
        ensure.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public async Task LateRenewalAcknowledgement_CannotResurrectLostOwnership()
    {
        var acknowledgement = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var lease = new CoreEx.Coordination.CoordinationLease(Options, System.Diagnostics.Stopwatch.GetTimestamp(), _ => acknowledgement.Task, _ => Task.CompletedTask, NullLogger.Instance);
        Func<Task> wait = () => Task.Delay(Timeout.InfiniteTimeSpan, lease.OwnershipLost).WaitAsync(TimeSpan.FromSeconds(3));
        await wait.Should().ThrowAsync<OperationCanceledException>();
        acknowledgement.SetResult(true);
        lease.OwnershipLost.IsCancellationRequested.Should().BeTrue();
    }

    [Test]
    public async Task Disposal_SerializesCleanupAfterInFlightCompletion()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var acknowledgement = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var released = false;
        var lease = new CoreEx.Coordination.CoordinationLease(Options, System.Diagnostics.Stopwatch.GetTimestamp(), _ => Task.FromResult(true), _ => { released = true; return Task.CompletedTask; }, NullLogger.Instance);
        var complete = lease.CompleteAsync(_ => { entered.SetResult(); return acknowledgement.Task; }, default);
        await entered.Task;
        var disposal = lease.DisposeAsync().AsTask();
        released.Should().BeFalse();
        acknowledgement.SetResult(true);
        await complete;
        await disposal;
        released.Should().BeTrue();
        lease.OwnershipLost.IsCancellationRequested.Should().BeTrue();
        await lease.DisposeAsync();
    }

    [Test]
    public async Task CleanupFailure_IsNotReportedAsSuccessfulRelease()
    {
        var lease = new CoreEx.Coordination.CoordinationLease(Options, System.Diagnostics.Stopwatch.GetTimestamp(), _ => Task.FromResult(true), _ => Task.FromException(new InvalidOperationException("backend unavailable")), NullLogger.Instance);
        Func<Task> dispose = () => lease.DisposeAsync().AsTask();
        await dispose.Should().ThrowAsync<InvalidOperationException>().WithMessage("backend unavailable");
        lease.OwnershipLost.IsCancellationRequested.Should().BeTrue();
    }
}
