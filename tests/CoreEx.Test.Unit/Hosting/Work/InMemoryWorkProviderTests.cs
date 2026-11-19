using CoreEx.Hosting.Work;

namespace CoreEx.Test.Unit.Hosting.Work;

[TestFixture]
public class InMemoryWorkProviderTests
{
    /// <summary>Provides a controllable clock for provider tests.</summary>
    private sealed class TestTimeProvider : TimeProvider
    {
        /// <summary>Gets or sets the current test time.</summary>
        public DateTimeOffset Now { get; set; } = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);

        /// <inheritdoc/>
        public override DateTimeOffset GetUtcNow() => Now;
    }

    [Test]
    public async Task Read_Returns_Provider_Time_For_Present_And_Absent_Records()
    {
        var time = new TestTimeProvider();
        var provider = new InMemoryWorkProvider(timeProvider: time);
        var absent = await provider.GetAsync("absent", default);
        absent.State.Should().BeNull();
        absent.Now.Should().Be(time.Now);

        var state = new WorkState { Id = "abc", TypeName = "T", Status = WorkStatus.Created };
        (await provider.CreateAsync(state, TimeSpan.FromMinutes(1), default)).Should().BeTrue();
        var present = await provider.GetAsync("abc", default);
        present.State!.ETag.Should().Be(state.ETag);
        present.Now.Should().Be(time.Now);
    }

    [Test]
    public async Task Update_And_Delete_Use_ETag()
    {
        var provider = new InMemoryWorkProvider();
        var state = new WorkState { Id = "abc", TypeName = "T", Status = WorkStatus.Created };
        (await provider.CreateAsync(state, TimeSpan.FromMinutes(1), default)).Should().BeTrue();
        (await provider.CreateAsync(new WorkState { Id = "abc", TypeName = "T" }, TimeSpan.FromMinutes(1), default)).Should().BeFalse();

        var first = (await provider.GetAsync("abc", default)).State!;
        var stale = (await provider.GetAsync("abc", default)).State!;
        first.Status = WorkStatus.Started;
        (await provider.UpdateAsync(first, TimeSpan.FromMinutes(1), default)).Should().BeTrue();
        first.ETag.Should().NotBe(stale.ETag);

        stale.Status = WorkStatus.Canceled;
        (await provider.UpdateAsync(stale, TimeSpan.FromMinutes(1), default)).Should().BeFalse();
        (await provider.DeleteAsync(stale, default)).Should().BeFalse();

        first.Status = WorkStatus.Completed;
        (await provider.UpdateAsync(first, TimeSpan.FromMinutes(1), default)).Should().BeTrue();
        (await provider.GetAsync("abc", default)).State!.Status.Should().Be(WorkStatus.Completed);

        (await provider.DeleteAsync(first, default)).Should().BeTrue();
        (await provider.GetAsync("abc", default)).State.Should().BeNull();
        (await provider.DeleteAsync(first, default)).Should().BeTrue();
    }

    [Test]
    public async Task Records_Expire_Using_The_Provider_Clock()
    {
        var time = new TestTimeProvider();
        var provider = new InMemoryWorkProvider(timeProvider: time);
        (await provider.CreateAsync(new WorkState { Id = "abc", TypeName = "T" }, TimeSpan.FromMinutes(1), default)).Should().BeTrue();
        time.Now = time.Now.AddMinutes(2);
        (await provider.GetAsync("abc", default)).State.Should().BeNull();
        (await provider.CreateAsync(new WorkState { Id = "abc", TypeName = "T" }, TimeSpan.FromMinutes(1), default)).Should().BeTrue();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task RecreatedRecord_Rejects_Previous_Incarnation(bool expire)
    {
        var time = new TestTimeProvider();
        var provider = new InMemoryWorkProvider(timeProvider: time);
        var original = new WorkState { Id = "abc", TypeName = "T" };
        var ttl = TimeSpan.FromMinutes(1);
        (await provider.CreateAsync(original, ttl, default)).Should().BeTrue();
        (await provider.UpdateAsync(original, ttl, default)).Should().BeTrue();
        var stale = (await provider.GetAsync("abc", default)).State!;

        if (expire)
            time.Now = time.Now.AddMinutes(2);
        else
            (await provider.DeleteAsync(original, default)).Should().BeTrue();

        var replacement = new WorkState { Id = "abc", TypeName = "T" };
        (await provider.CreateAsync(replacement, ttl, default)).Should().BeTrue();
        (await provider.UpdateAsync(replacement, ttl, default)).Should().BeTrue();
        replacement.ETag.Should().NotBe(stale.ETag);
        (await provider.UpdateAsync(stale, ttl, default)).Should().BeFalse();
        (await provider.DeleteAsync(stale, default)).Should().BeFalse();
        (await provider.GetAsync("abc", default)).State!.ETag.Should().Be(replacement.ETag);
    }
}
