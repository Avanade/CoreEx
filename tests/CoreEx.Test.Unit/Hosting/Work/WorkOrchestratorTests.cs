using CoreEx.Hosting.Work;
using CoreEx.Caching;

namespace CoreEx.Test.Unit.Hosting.Work;

[TestFixture]
public class WorkOrchestratorTests
{
    [Test]
    public async Task Orchestrate_End_To_End()
    {
        var p = new HybridCacheWorkProvider(new MemoryOnlyHybridCache());
        var o = new WorkOrchestrator(p);

        ExecutionContext.Reset();
        var ec = ExecutionContext.Current;

        var wa = new WorkArgs("Test-Work", "abc");

        // Start work.
        var ws = await o.CreateAsync(wa);
        ws.Should().NotBeNull();
        ws.TypeName.Should().Be("Test-Work");
        ws.Id.Should().Be("abc");
        ws.Status.Should().Be(WorkStatus.Created);
        ws.Created.Should().Be(ec.Timestamp);
        ws.Started.Should().BeNull();
        ws.Indeterminate.Should().BeNull();
        ws.Finished.Should().BeNull();
        ws.Reason.Should().BeNull();

        // Get not found.
        var ws2 = await o.GetWithTypeAsync("Test-Work", "def");
        ws2.Should().BeNull();

        // Get found.
        ws2 = await o.GetWithTypeAsync("Test-Work", "abc");
        ObjectComparer.Assert(ws, ws2);

        // Start work.
        ws = await o.StartAsync("abc");
        ws.Should().NotBeNull();
        ws.TypeName.Should().Be("Test-Work");
        ws.Id.Should().Be("abc");
        ws.Status.Should().Be(WorkStatus.Started);
        ws.Created.Should().Be(ec.Timestamp);
        ws.Started.Should().Be(ec.Timestamp);
        ws.Indeterminate.Should().BeNull();
        ws.Finished.Should().BeNull();
        ws.Reason.Should().BeNull();

        // Indeterminate work.
        ws = await o.IndeterminateAsync("abc", "Not sure!");
        ws.Should().NotBeNull();
        ws.TypeName.Should().Be("Test-Work");
        ws.Id.Should().Be("abc");
        ws.Status.Should().Be(WorkStatus.Indeterminate);
        ws.Created.Should().Be(ec.Timestamp);
        ws.Started.Should().Be(ec.Timestamp);
        ws.Indeterminate.Should().Be(ec.Timestamp);
        ws.Finished.Should().BeNull();
        ws.Reason.Should().Be("Not sure!");

        // Get data.
        var bd = await o.GetDataAsync("abc");
        bd.Should().BeNull();

        // Set data.
        await o.SetDataValueAsync("abc", "123");

        // Get data.
        var dv = await o.GetDataValueAsync<string>("abc");
        dv.Should().Be("123");

        // Complete work.
        ws = await o.CompleteAsync("abc");
        ws.Should().NotBeNull();
        ws.TypeName.Should().Be("Test-Work");
        ws.Id.Should().Be("abc");
        ws.Status.Should().Be(WorkStatus.Completed);
        ws.Created.Should().Be(ec.Timestamp);
        ws.Started.Should().Be(ec.Timestamp);
        ws.Indeterminate.Should().Be(ec.Timestamp);
        ws.Finished.Should().Be(ec.Timestamp);
        ws.Reason.Should().BeNull();

        // Get different type with same id.
        ws = await o.GetWithTypeAsync<string>("abc");
        ws.Should().BeNull();

        // Get correct type with same id, but different user.
        ExecutionContext.Current.User = Security.AuthenticationUser.Anonymous;
        ws = await o.GetWithTypeAsync("Test-Work", "abc");
        ws.Should().BeNull();
    }

    [Test]
    public async Task Auto_Expire_On_Get()
    {
        var p = new HybridCacheWorkProvider(new MemoryOnlyHybridCache());
        var o = new WorkOrchestrator(p);

        ExecutionContext.Reset();
        var ec = ExecutionContext.Current;

        var wa = new WorkArgs("Test-Work", "abc") { Expiry = TimeSpan.FromDays(-1) };

        // Start work.
        var ws = await o.CreateAsync(wa);
        ws.Should().NotBeNull();

        // Get work - should be expired.
        ws = await o.GetAsync("abc");
        ws.Should().NotBeNull();
        ws.Status.Should().Be(WorkStatus.Expired);
        ws.Created.Should().Be(ec.Timestamp);
        ws.Started.Should().BeNull();
        ws.Indeterminate.Should().BeNull();
        ws.Finished.Should().Be(ec.Timestamp);
    }

    [Test]
    public async Task Create_Then_Fail()
    {
        var p = new HybridCacheWorkProvider(new MemoryOnlyHybridCache());
        var o = new WorkOrchestrator(p);

        ExecutionContext.Reset();
        var ec = ExecutionContext.Current;

        var wa = new WorkArgs("Test-Work", "abc");

        // Create work.
        var ws = await o.CreateAsync(wa);
        ws.Should().NotBeNull();

        // Start work.
        ws = await o.StartAsync("abc");
        ws.Should().NotBeNull();

        // Fail work.
        ws = await o.FailAsync("abc", "Because I said so!");
        ws.Should().NotBeNull();
        ws.TypeName.Should().Be("Test-Work");
        ws.Id.Should().Be("abc");
        ws.Status.Should().Be(WorkStatus.Failed);
        ws.Created.Should().Be(ec.Timestamp);
        ws.Started.Should().Be(ec.Timestamp);
        ws.Indeterminate.Should().BeNull();
        ws.Finished.Should().Be(ec.Timestamp);
        ws.Reason.Should().Be("Because I said so!");
    }

    [Test]
    public async Task Create_Then_Cancel()
    {
        var p = new HybridCacheWorkProvider(new MemoryOnlyHybridCache());
        var o = new WorkOrchestrator(p);

        ExecutionContext.Reset();
        var ec = ExecutionContext.Current;

        var wa = new WorkArgs("Test-Work", "abc");

        // Start work.
        var ws = await o.CreateAsync(wa);
        ws.Should().NotBeNull();

        // Cancel work.
        ws = await o.CancelAsync("abc", "Not needed!");
        ws.Should().NotBeNull();
        ws.TypeName.Should().Be("Test-Work");
        ws.Id.Should().Be("abc");
        ws.Status.Should().Be(WorkStatus.Canceled);
        ws.Created.Should().Be(ec.Timestamp);
        ws.Started.Should().BeNull();
        ws.Indeterminate.Should().BeNull();
        ws.Finished.Should().Be(ec.Timestamp);
        ws.Reason.Should().Be("Not needed!");
    }
}