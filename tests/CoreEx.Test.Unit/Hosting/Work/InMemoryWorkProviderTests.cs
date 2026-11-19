using CoreEx.Hosting.Work;

namespace CoreEx.Test.Unit.Hosting.Work;

[TestFixture]
public class InMemoryWorkProviderTests
{
    private sealed class TestTimeProvider : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;

        public override DateTimeOffset GetUtcNow() => Now;
    }

    [Test]
    public async Task Update_And_Delete_Use_ETag()
    {
        var p = new InMemoryWorkProvider();
        var ws = new WorkState { Id = "abc", TypeName = "T", Status = WorkStatus.Created };
        (await p.CreateAsync(ws, TimeSpan.FromMinutes(1), default)).Should().BeTrue();
        (await p.CreateAsync(new WorkState { Id = "abc", TypeName = "T" }, TimeSpan.FromMinutes(1), default)).Should().BeFalse();

        var a = (await p.GetAsync("abc", default))!;
        var b = (await p.GetAsync("abc", default))!;
        a.Status = WorkStatus.Started;
        (await p.UpdateAsync(a, BinaryData.FromString("x"), TimeSpan.FromMinutes(1), default)).Should().BeTrue();
        a.ETag.Should().NotBe(b.ETag);

        b.Status = WorkStatus.Canceled;
        (await p.UpdateAsync(b, null, TimeSpan.FromMinutes(1), default)).Should().BeFalse();
        (await p.DeleteAsync(b, default)).Should().BeFalse();

        a.Status = WorkStatus.Completed;
        (await p.UpdateAsync(a, null, TimeSpan.FromMinutes(1), default)).Should().BeTrue();
        (await p.GetDataAsync("abc", default))!.ToString().Should().Be("x");
        (await p.GetAsync("abc", default))!.Status.Should().Be(WorkStatus.Completed);

        (await p.DeleteAsync(a, default)).Should().BeTrue();
        (await p.GetAsync("abc", default)).Should().BeNull();
        (await p.DeleteAsync(a, default)).Should().BeTrue();
    }

    [Test]
    public async Task Records_Expire()
    {
        var tp = new TestTimeProvider();
        var p = new InMemoryWorkProvider(timeProvider: tp);
        (await p.CreateAsync(new WorkState { Id = "abc", TypeName = "T" }, TimeSpan.FromMinutes(1), default)).Should().BeTrue();
        tp.Now = tp.Now.AddMinutes(2);
        (await p.GetAsync("abc", default)).Should().BeNull();
        (await p.CreateAsync(new WorkState { Id = "abc", TypeName = "T" }, TimeSpan.FromMinutes(1), default)).Should().BeTrue();
    }
}
