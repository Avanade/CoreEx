using CoreEx.Hosting.Work;

namespace CoreEx.Caching.Redis.Test.Unit;

[TestFixture]
public class RedisWorkProviderTests
{
    private StackExchange.Redis.ConnectionMultiplexer _first = null!;
    private StackExchange.Redis.ConnectionMultiplexer _second = null!;

    [OneTimeSetUp]
    public async Task SetUp()
    {
        var connection = Environment.GetEnvironmentVariable("COREEX_REDIS_TEST_CONNECTION") ?? "localhost:6379";
        _first = await StackExchange.Redis.ConnectionMultiplexer.ConnectAsync(connection);
        _second = await StackExchange.Redis.ConnectionMultiplexer.ConnectAsync(connection);
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _first.Dispose();
        _second.Dispose();
    }

    [Test]
    public async Task IndependentClients_CreateAndStartHaveSingleWinner()
    {
        var id = Guid.NewGuid().ToString();
        var a = new WorkOrchestrator(new CoreEx.Redis.RedisWorkProvider(_first));
        var b = new WorkOrchestrator(new CoreEx.Redis.RedisWorkProvider(_second));

        var creates = await Task.WhenAll(a.CreateAsync(new WorkArgs("T", id)), b.CreateAsync(new WorkArgs("T", id)));
        creates.Count(r => r.IsSuccess).Should().Be(1);
        creates.Count(r => r.IsFailureOfType<ConflictException>()).Should().Be(1);

        var starts = await Task.WhenAll(a.StartAsync(id), b.StartAsync(id));
        starts.Count(r => r.IsSuccess).Should().Be(1);
        starts.Count(r => r.IsFailureOfType<ConflictException>()).Should().Be(1);

        (await a.CompleteAsync(id, BinaryData.FromString("done"))).Value.Status.Should().Be(WorkStatus.Completed);
        (await b.CancelAsync(id, "late")).Value.Status.Should().Be(WorkStatus.Completed);
        (await b.GetDataAsync(id))!.ToString().Should().Be("done");
        (await b.DeleteAsync(id)).IsSuccess.Should().BeTrue();
        (await a.GetAsync(id)).Should().BeNull();
    }

    [Test]
    public async Task StaleETag_IsRejected_AndDataIsKeptUnlessSet()
    {
        var p = new CoreEx.Redis.RedisWorkProvider(_first);
        var id = Guid.NewGuid().ToString();
        (await p.CreateAsync(new WorkState { Id = id, TypeName = "T", Status = WorkStatus.Created }, TimeSpan.FromMinutes(1), default)).Should().BeTrue();

        var a = (await p.GetAsync(id, default))!;
        var b = (await p.GetAsync(id, default))!;
        a.Status = WorkStatus.Started;
        (await p.UpdateAsync(a, BinaryData.FromString("x"), TimeSpan.FromMinutes(1), default)).Should().BeTrue();
        (await p.UpdateAsync(b, null, TimeSpan.FromMinutes(1), default)).Should().BeFalse();
        (await p.DeleteAsync(b, default)).Should().BeFalse();

        a.Status = WorkStatus.Completed;
        (await p.UpdateAsync(a, null, TimeSpan.FromMinutes(1), default)).Should().BeTrue();
        (await p.GetDataAsync(id, default))!.ToString().Should().Be("x");
        var c = (await p.GetAsync(id, default))!;
        c.Status.Should().Be(WorkStatus.Completed);
        c.ETag.Should().Be(a.ETag);

        (await p.DeleteAsync(c, default)).Should().BeTrue();
        (await p.DeleteAsync(c, default)).Should().BeTrue();
    }

    [Test]
    public async Task Records_Expire_AndOversizedDataIsRejected()
    {
        var p = new CoreEx.Redis.RedisWorkProvider(_first, maxDataSize: 4);
        var id = Guid.NewGuid().ToString();
        var ws = new WorkState { Id = id, TypeName = "T", Status = WorkStatus.Created };
        (await p.CreateAsync(ws, TimeSpan.FromMilliseconds(200), default)).Should().BeTrue();
        await FluentActions.Awaiting(() => p.UpdateAsync(ws, BinaryData.FromString("too large"), TimeSpan.FromMinutes(1), default)).Should().ThrowAsync<ArgumentException>();
        await Task.Delay(400);
        (await p.GetAsync(id, default)).Should().BeNull();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task RecreatedRecord_RejectsPreviousIncarnation(bool expire)
    {
        var first = new CoreEx.Redis.RedisWorkProvider(_first);
        var second = new CoreEx.Redis.RedisWorkProvider(_second);
        var id = Guid.NewGuid().ToString();
        var ttl = TimeSpan.FromMinutes(1);
        var original = new WorkState { Id = id, TypeName = "T" };
        (await first.CreateAsync(original, ttl, default)).Should().BeTrue();
        (await first.UpdateAsync(original, null, expire ? TimeSpan.FromSeconds(1) : ttl, default)).Should().BeTrue();
        var stale = (await second.GetAsync(id, default))!;

        if (expire)
        {
            for (var attempt = 0; attempt < 50 && await first.GetAsync(id, default) is not null; attempt++)
                await Task.Delay(100);

            (await first.GetAsync(id, default)).Should().BeNull();
        }
        else
            (await first.DeleteAsync(original, default)).Should().BeTrue();

        var replacement = new WorkState { Id = id, TypeName = "T" };
        (await second.CreateAsync(replacement, ttl, default)).Should().BeTrue();
        (await second.UpdateAsync(replacement, BinaryData.FromString("replacement"), ttl, default)).Should().BeTrue();
        replacement.ETag.Should().NotBe(stale.ETag);
        (await first.UpdateAsync(stale, BinaryData.FromString("stale"), ttl, default)).Should().BeFalse();
        (await first.DeleteAsync(stale, default)).Should().BeFalse();
        (await first.GetAsync(id, default))!.ETag.Should().Be(replacement.ETag);
        (await first.GetDataAsync(id, default))!.ToString().Should().Be("replacement");
        (await second.DeleteAsync(replacement, default)).Should().BeTrue();
    }
}
