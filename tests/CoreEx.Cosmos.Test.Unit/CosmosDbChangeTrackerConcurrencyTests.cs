namespace CoreEx.Cosmos.Test.Unit;

[TestFixture]
public class CosmosDbChangeTrackerConcurrencyTests
{
    [TestCase(false)]
    [TestCase(true)]
    public async Task DelayedRead_AfterInvalidation_CannotReplaceNewSnapshot(bool clear)
    {
        var tracker = new CosmosDbChangeTracker();
        var options = new System.Text.Json.JsonSerializerOptions();
        var partitionKey = new PartitionKey("partition");
        var oldVersion = tracker.CaptureVersion("items", partitionKey, "id");
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var oldRead = Task.Run(async () =>
        {
            await release.Task.ConfigureAwait(false);
            tracker.Set(options, "items", partitionKey, "id", new TestItem { Name = "Old" }, oldVersion);
        });

        if (clear)
            tracker.Clear();
        else
            tracker.Remove("items", partitionKey, "id");

        var newVersion = tracker.CaptureVersion("items", partitionKey, "id");
        tracker.Set(options, "items", partitionKey, "id", new TestItem { Name = "New" }, newVersion);
        release.SetResult();
        await oldRead.ConfigureAwait(false);

        tracker.Count.Should().Be(1);
        tracker.TryGet<TestItem>(options, "items", partitionKey, "id", out var item).Should().BeTrue();
        item!.Name.Should().Be("New");
    }

    [Test]
    public void ReadStartedDuringWrite_IsInvalidatedAtWriteCompletion()
    {
        var tracker = new CosmosDbChangeTracker();
        var options = new System.Text.Json.JsonSerializerOptions();
        var partitionKey = new PartitionKey("partition");
        tracker.Remove("items", partitionKey, "id");
        var version = tracker.CaptureVersion("items", partitionKey, "id");

        tracker.Remove("items", partitionKey, "id");
        tracker.Set(options, "items", partitionKey, "id", new TestItem { Name = "Old" }, version);

        tracker.Count.Should().Be(0);
    }

    [Test]
    public void Invalidation_CoversAllModelTypes_ButNotOtherDocuments()
    {
        var tracker = new CosmosDbChangeTracker();
        var options = new System.Text.Json.JsonSerializerOptions();
        var partitionKey = new PartitionKey("partition");
        var version = tracker.CaptureVersion("items", partitionKey, "id");
        var otherVersion = tracker.CaptureVersion("items", partitionKey, "other");
        tracker.Set(options, "items", partitionKey, "id", new TestItem(), version);
        tracker.Set(options, "items", partitionKey, "id", new object(), version);

        tracker.Remove("items", partitionKey, "id");
        tracker.Set(options, "items", partitionKey, "id", new TestItem(), version);
        tracker.Set(options, "items", partitionKey, "id", new object(), version);
        tracker.Set(options, "items", partitionKey, "other", new TestItem(), otherVersion);

        tracker.Count.Should().Be(1);
        tracker.TryGet<TestItem>(options, "items", partitionKey, "other", out _).Should().BeTrue();
    }
}
