using CoreEx.Hosting.Work;
using CoreEx.Security;
using System.Diagnostics;

namespace CoreEx.Test.Unit.Hosting.Work;

[TestFixture]
public class WorkOrchestratorTests
{
    private static readonly AuthenticationUser _user = new() { Type = AuthenticationType.AccountUser, UserName = "bob", Id = "123" };

    private static WorkOrchestrator Create(IWorkProvider? provider = null)
    {
        ExecutionContext.Reset();
        ExecutionContext.Current.User = _user;
        return new WorkOrchestrator(provider ?? new InMemoryWorkProvider());
    }

    [Test]
    public async Task Orchestrate_End_To_End()
    {
        var o = Create();
        var ec = ExecutionContext.Current;

        var ws = (await o.CreateAsync(new WorkArgs("Test-Work", "abc"))).Value;
        ws.TypeName.Should().Be("Test-Work");
        ws.Id.Should().Be("abc");
        ws.Status.Should().Be(WorkStatus.Created);
        ws.CreatedOn.Should().Be(ec.Timestamp);
        ws.User.Should().Be(_user);
        ws.ETag.Should().NotBeNull();
        ws.StartedOn.Should().BeNull();
        ws.FinishedOn.Should().BeNull();

        (await o.GetWithTypeAsync("Test-Work", "def")).Should().BeNull();
        ObjectComparer.Assert(ws, await o.GetWithTypeAsync("Test-Work", "abc"));

        ws = (await o.StartAsync("abc")).Value;
        ws.Status.Should().Be(WorkStatus.Started);
        ws.StartedOn.Should().Be(ec.Timestamp);

        ws = (await o.IndeterminateAsync("abc", "Not sure!")).Value;
        ws.Status.Should().Be(WorkStatus.Indeterminate);
        ws.IndeterminateOn.Should().Be(ec.Timestamp);
        ws.Reason.Should().Be("Not sure!");

        (await o.GetDataAsync("abc")).Should().BeNull();
        (await o.SetDataValueAsync("abc", "123")).IsSuccess.Should().BeTrue();
        (await o.GetDataValueAsync<string>("abc")).Should().Be("123");

        ws = (await o.CompleteAsync("abc")).Value;
        ws.Status.Should().Be(WorkStatus.Completed);
        ws.FinishedOn.Should().Be(ec.Timestamp);
        ws.Reason.Should().BeNull();

        (await o.GetWithTypeAsync<string>("abc")).Should().BeNull();

        ExecutionContext.Current.User = AuthenticationUser.Anonymous;
        (await o.GetWithTypeAsync("Test-Work", "abc")).Should().BeNull();
    }

    [Test]
    public async Task Auto_Expire_On_Get()
    {
        var o = Create();
        var ec = ExecutionContext.Current;

        (await o.CreateAsync(new WorkArgs("Test-Work", "abc") { Expiry = TimeSpan.FromDays(-1) })).IsSuccess.Should().BeTrue();

        var ws = await o.GetAsync("abc");
        ws.Should().NotBeNull();
        ws.Status.Should().Be(WorkStatus.Expired);
        ws.FinishedOn.Should().Be(ec.Timestamp);
    }

    [Test]
    public async Task Create_Then_Fail()
    {
        var o = Create();
        (await o.CreateAsync(new WorkArgs("Test-Work", "abc"))).IsSuccess.Should().BeTrue();
        (await o.StartAsync("abc")).IsSuccess.Should().BeTrue();

        var ws = (await o.FailAsync("abc", "Because I said so!")).Value;
        ws.Status.Should().Be(WorkStatus.Failed);
        ws.Reason.Should().Be("Because I said so!");

        ws = (await o.FailAsync("abc", new InvalidOperationException("secret"))).Value;
        ws.Reason.Should().Be("Because I said so!", "a finished work is not updated");
    }

    [Test]
    public async Task Fail_With_Exception_Does_Not_Persist_Message()
    {
        var o = Create();
        (await o.CreateAsync(new WorkArgs("Test-Work", "abc"))).IsSuccess.Should().BeTrue();
        (await o.StartAsync("abc")).IsSuccess.Should().BeTrue();

        var ws = (await o.FailAsync("abc", new InvalidOperationException("secret"))).Value;
        ws.Status.Should().Be(WorkStatus.Failed);
        ws.Reason.Should().NotContain("secret");
    }

    [Test]
    public async Task Create_Then_Cancel()
    {
        var o = Create();
        (await o.CreateAsync(new WorkArgs("Test-Work", "abc"))).IsSuccess.Should().BeTrue();

        var ws = (await o.CancelAsync("abc", "Not needed!")).Value;
        ws.Status.Should().Be(WorkStatus.Canceled);
        ws.StartedOn.Should().BeNull();
        ws.Reason.Should().Be("Not needed!");
    }

    [Test]
    public async Task Create_Duplicate_Conflicts()
    {
        var o = Create();
        (await o.CreateAsync(new WorkArgs("Test-Work", "abc"))).IsSuccess.Should().BeTrue();
        (await o.CreateAsync(new WorkArgs("Test-Work", "abc"))).IsFailureOfType<ConflictException>().Should().BeTrue();
    }

    [Test]
    public async Task Concurrent_Create_And_Start_Have_Single_Winner()
    {
        var o = Create();
        var creates = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => o.CreateAsync(new WorkArgs("Test-Work", "abc")))));
        creates.Count(r => r.IsSuccess).Should().Be(1);
        creates.Count(r => r.IsFailureOfType<ConflictException>()).Should().Be(19);

        var starts = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => o.StartAsync("abc"))));
        starts.Count(r => r.IsSuccess).Should().Be(1);
        starts.Count(r => r.IsFailureOfType<ConflictException>()).Should().Be(19);
    }

    [Test]
    public async Task Status_Transitions_Are_Guarded()
    {
        var o = Create();
        (await o.StartAsync("abc")).IsFailureOfType<NotFoundException>().Should().BeTrue();
        (await o.CreateAsync(new WorkArgs("Test-Work", "abc"))).IsSuccess.Should().BeTrue();
        (await o.CompleteAsync("abc")).IsFailureOfType<ConflictException>().Should().BeTrue();
        (await o.DeleteAsync("abc")).IsFailureOfType<ConflictException>().Should().BeTrue();
        (await o.StartAsync("abc")).IsSuccess.Should().BeTrue();
        (await o.StartAsync("abc")).IsFailureOfType<ConflictException>().Should().BeTrue();
        (await o.CompleteAsync("abc", BinaryData.FromString("done"))).Value.Status.Should().Be(WorkStatus.Completed);

        // Finished work is never overwritten.
        (await o.CancelAsync("abc", "late")).Value.Status.Should().Be(WorkStatus.Completed);
        (await o.ExpireAsync("abc", "late")).Value.Status.Should().Be(WorkStatus.Completed);
        (await o.SetDataAsync("abc", BinaryData.FromString("other"))).IsFailureOfType<ConflictException>().Should().BeTrue();
        (await o.GetDataAsync("abc"))!.ToString().Should().Be("done");

        (await o.DeleteAsync("abc")).IsSuccess.Should().BeTrue();
        (await o.GetAsync("abc")).Should().BeNull();
        (await o.DeleteAsync("abc")).IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task Get_Returns_Independent_Copy()
    {
        var o = Create();
        (await o.CreateAsync(new WorkArgs("Test-Work", "abc"))).IsSuccess.Should().BeTrue();
        var ws = await o.GetAsync("abc");
        ws!.Status = WorkStatus.Completed;
        (await o.GetAsync("abc"))!.Status.Should().Be(WorkStatus.Created);
    }

    [Test]
    public async Task Create_Captures_Current_Activity()
    {
        var o = Create();
        using var activity = new Activity("work").SetIdFormat(ActivityIdFormat.W3C).Start();
        activity.TraceStateString = "k=v";

        var ws = (await o.CreateAsync(new WorkArgs("Test-Work", "abc"))).Value;
        ws.TraceParent.Should().Be(activity.Id);
        ws.TraceState.Should().Be("k=v");
        (await o.GetAsync("abc"))!.TraceParent.Should().Be(activity.Id);
    }

    [Test]
    public void WorkState_TimestampRenames_PreserveSerializedNames()
    {
        var created = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var state = new WorkState
        {
            CreatedOn = created,
            ExpiryOn = created.AddDays(1),
            StartedOn = created.AddMinutes(1),
            IndeterminateOn = created.AddMinutes(2),
            FinishedOn = created.AddMinutes(3)
        };

        var json = JsonSerializer.SerializeToElement(state, JsonDefaults.SerializerOptions);
        json.GetProperty("created").GetDateTimeOffset().Should().Be(state.CreatedOn);
        json.GetProperty("expiry").GetDateTimeOffset().Should().Be(state.ExpiryOn);
        json.GetProperty("started").GetDateTimeOffset().Should().Be(state.StartedOn);
        json.GetProperty("indeterminate").GetDateTimeOffset().Should().Be(state.IndeterminateOn);
        json.GetProperty("finished").GetDateTimeOffset().Should().Be(state.FinishedOn);
        json.TryGetProperty("createdOn", out _).Should().BeFalse();

        var restored = JsonSerializer.Deserialize<WorkState>(
            """{"created":"2026-01-02T03:04:05+00:00","expiry":"2026-01-03T03:04:05+00:00","started":"2026-01-02T03:05:05+00:00","indeterminate":"2026-01-02T03:06:05+00:00","finished":"2026-01-02T03:07:05+00:00"}""",
            JsonDefaults.SerializerOptions)!;
        restored.CreatedOn.Should().Be(state.CreatedOn);
        restored.ExpiryOn.Should().Be(state.ExpiryOn);
        restored.StartedOn.Should().Be(state.StartedOn);
        restored.IndeterminateOn.Should().Be(state.IndeterminateOn);
        restored.FinishedOn.Should().Be(state.FinishedOn);
    }
}
