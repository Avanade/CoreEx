using CoreEx.Hosting.Work;
using CoreEx.Security;
using System.Diagnostics;

namespace CoreEx.Test.Unit.Hosting.Work;

[TestFixture]
public class WorkOrchestratorTests
{
    private static readonly DateTimeOffset _now = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    private static readonly AuthenticationUser _user = new() { Type = AuthenticationType.AccountUser, UserName = "bob", Id = "123" };

    private static OperationKey Key(string typeName, string id) => OperationKey.ForDomain("CoreEx.Test.Unit", typeName, id);

    private static WorkArgs Args(string typeName, string id) => new(typeName, Key(typeName, id));

    /// <summary>Provides a controllable clock for work tests.</summary>
    private sealed class ManualTimeProvider(DateTimeOffset now) : TimeProvider
    {
        /// <summary>Gets or sets the current test time.</summary>
        public DateTimeOffset Now { get; set; } = now;

        /// <inheritdoc/>
        public override DateTimeOffset GetUtcNow() => Now;
    }

    /// <summary>Simulates an ambiguous provider update for read-back tests.</summary>
    private sealed class AmbiguousProvider(IWorkProvider inner, bool applyBeforeThrow) : IWorkProvider
    {
        private bool _throwNextUpdate = true;

        /// <inheritdoc/>
        public Task<WorkSnapshot> GetAsync(OperationKey operationKey, CancellationToken cancellationToken) => inner.GetAsync(operationKey, cancellationToken);

        /// <inheritdoc/>
        public Task<bool> CreateAsync(WorkState state, TimeSpan timeToLive, CancellationToken cancellationToken) => inner.CreateAsync(state, timeToLive, cancellationToken);

        /// <inheritdoc/>
        public async Task<bool> UpdateAsync(WorkState state, TimeSpan timeToLive, CancellationToken cancellationToken)
        {
            if (!_throwNextUpdate)
                return await inner.UpdateAsync(state, timeToLive, cancellationToken).ConfigureAwait(false);

            _throwNextUpdate = false;
            if (applyBeforeThrow)
                await inner.UpdateAsync(state, timeToLive, cancellationToken).ConfigureAwait(false);

            throw new WorkOutcomeUnknownException(state.OperationKey, "update");
        }

        /// <inheritdoc/>
        public Task<bool> DeleteAsync(WorkState state, CancellationToken cancellationToken) => inner.DeleteAsync(state, cancellationToken);
    }

    /// <summary>Records the time-to-live requested for each provider write.</summary>
    private sealed class RecordingProvider(IWorkProvider inner) : IWorkProvider
    {
        /// <summary>Gets the most recently requested time-to-live.</summary>
        public TimeSpan LastTimeToLive { get; private set; }

        /// <inheritdoc/>
        public Task<WorkSnapshot> GetAsync(OperationKey operationKey, CancellationToken cancellationToken) => inner.GetAsync(operationKey, cancellationToken);

        /// <inheritdoc/>
        public Task<bool> CreateAsync(WorkState state, TimeSpan timeToLive, CancellationToken cancellationToken)
        {
            LastTimeToLive = timeToLive;
            return inner.CreateAsync(state, timeToLive, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<bool> UpdateAsync(WorkState state, TimeSpan timeToLive, CancellationToken cancellationToken)
        {
            LastTimeToLive = timeToLive;
            return inner.UpdateAsync(state, timeToLive, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<bool> DeleteAsync(WorkState state, CancellationToken cancellationToken) => inner.DeleteAsync(state, cancellationToken);
    }

    /// <summary>Creates an orchestrator with the test execution user.</summary>
    private static WorkOrchestrator Create(TimeProvider? timeProvider = null, WorkOrchestratorOptions? options = null, IWorkProvider? provider = null)
    {
        ExecutionContext.Reset();
        ExecutionContext.Current.User = _user;
        return new WorkOrchestrator(provider ?? new InMemoryWorkProvider(timeProvider: timeProvider), options);
    }

    [Test]
    public async Task Lifecycle_Uses_Provider_Clock_And_Stores_Only_Resource_Location()
    {
        var clock = new ManualTimeProvider(_now);
        var orchestrator = Create(clock);
        ExecutionContext.Current.Timestamp = _now.AddYears(-1);
        using var activity = new Activity("work").SetIdFormat(ActivityIdFormat.W3C).Start();

        var created = (await orchestrator.CreateAsync(Args("Test-Work", "abc"))).Value;
        created.TypeName.Should().Be("Test-Work");
        created.OperationKey.Should().Be(Key("Test-Work", "abc"));
        created.Id.Should().Be("abc");
        created.Status.Should().Be(WorkStatus.Created);
        created.CreatedOn.Should().Be(_now);
        created.User.Should().Be(_user);
        created.TraceParent.Should().Be(activity.Id);

        var started = await orchestrator.StartAsync(Key("Test-Work", "abc"), "worker-a");
        started.Outcome.Should().Be(WorkTransitionOutcome.Applied);
        started.Lease.Should().NotBeNull();
        started.State!.StartedOn.Should().Be(_now);

        clock.Now = _now.AddMinutes(2);
        var progress = await orchestrator.ReportProgressAsync(started.Lease!, new WorkProgress(40, "Working", default));
        progress.Outcome.Should().Be(WorkTransitionOutcome.Applied);
        progress.State!.Progress.Should().Be(new WorkProgress(40, "Working", clock.Now));

        var completed = await orchestrator.CompleteAsync(started.Lease!, "/products/123?v=2");
        completed.Outcome.Should().Be(WorkTransitionOutcome.Applied);
        completed.State!.Status.Should().Be(WorkStatus.Completed);
        completed.State.FinishedOn.Should().Be(clock.Now);
        completed.State.ResourceLocation.Should().Be("/products/123?v=2");
        completed.State.Error.Should().BeNull();
    }

    [Test]
    public async Task Concurrent_Start_Has_One_Lease_Winner()
    {
        var orchestrator = Create(new ManualTimeProvider(_now));
        (await orchestrator.CreateAsync(Args("T", "abc"))).IsSuccess.Should().BeTrue();

        var starts = await Task.WhenAll(Enumerable.Range(0, 20).Select(i => orchestrator.StartAsync(Key("T", "abc"), $"worker-{i}")));
        starts.Count(result => result.Outcome == WorkTransitionOutcome.Applied).Should().Be(1);
        starts.Count(result => result.Outcome == WorkTransitionOutcome.LeaseHeld).Should().Be(19);
    }

    [Test]
    public async Task Takeover_Increments_Epoch_And_Rejects_Stale_Lease()
    {
        var clock = new ManualTimeProvider(_now);
        var orchestrator = Create(clock, new WorkOrchestratorOptions { Expiry = TimeSpan.FromHours(1), LeaseDuration = TimeSpan.FromSeconds(30) });
        (await orchestrator.CreateAsync(Args("T", "abc"))).IsSuccess.Should().BeTrue();
        var first = (await orchestrator.StartAsync(Key("T", "abc"), "worker-a")).Lease!;
        first.Epoch.Should().Be(1);

        clock.Now = clock.Now.AddSeconds(31);
        var takeover = await orchestrator.StartAsync(Key("T", "abc"), "worker-b");
        takeover.Outcome.Should().Be(WorkTransitionOutcome.Applied);
        takeover.IsTakeover.Should().BeTrue();
        takeover.Lease!.Epoch.Should().Be(2);
        takeover.State!.Status.Should().Be(WorkStatus.Indeterminate);

        (await orchestrator.RenewLeaseAsync(first)).Outcome.Should().Be(WorkTransitionOutcome.LeaseLost);
        (await orchestrator.ReleaseLeaseAsync(first)).Outcome.Should().Be(WorkTransitionOutcome.LeaseLost);
        (await orchestrator.ReportProgressAsync(first, new WorkProgress(1, null, default))).Outcome.Should().Be(WorkTransitionOutcome.LeaseLost);
        (await orchestrator.CompleteAsync(first)).Outcome.Should().Be(WorkTransitionOutcome.LeaseLost);
        (await orchestrator.FailAsync(first, new WorkError(WorkErrorCodes.ExecutionFailed, "failed"))).Outcome.Should().Be(WorkTransitionOutcome.LeaseLost);

        (await orchestrator.CompleteAsync(takeover.Lease!)).Outcome.Should().Be(WorkTransitionOutcome.Applied);
    }

    [Test]
    public async Task Expired_Lease_Can_Complete_Until_A_Takeover_Occurs()
    {
        var clock = new ManualTimeProvider(_now);
        var orchestrator = Create(clock, new WorkOrchestratorOptions { Expiry = TimeSpan.FromHours(1), LeaseDuration = TimeSpan.FromSeconds(30) });
        (await orchestrator.CreateAsync(Args("T", "abc"))).IsSuccess.Should().BeTrue();
        var lease = (await orchestrator.StartAsync(Key("T", "abc"), "worker-a")).Lease!;
        clock.Now = clock.Now.AddSeconds(31);

        (await orchestrator.CompleteAsync(lease)).Outcome.Should().Be(WorkTransitionOutcome.Applied);
    }

    [Test]
    public async Task StartBy_And_Expiry_Persist_Terminal_Unknown_States()
    {
        var clock = new ManualTimeProvider(_now);
        var orchestrator = Create(clock, new WorkOrchestratorOptions { Expiry = TimeSpan.FromHours(1), LeaseDuration = TimeSpan.FromMinutes(1) });
        var notStartedArgs = Args("T", "not-started");
        notStartedArgs.StartTimeout = TimeSpan.FromSeconds(15);
        (await orchestrator.CreateAsync(notStartedArgs)).IsSuccess.Should().BeTrue();
        clock.Now = clock.Now.AddSeconds(15);

        var notStarted = await orchestrator.StartAsync(Key("T", "not-started"), "worker-a");
        notStarted.Outcome.Should().Be(WorkTransitionOutcome.Expired);
        notStarted.State!.Status.Should().Be(WorkStatus.Expired);
        notStarted.State.Error!.Code.Should().Be(WorkErrorCodes.NotStarted);

        var shortLived = new WorkOrchestrator(new InMemoryWorkProvider(timeProvider: clock), new WorkOrchestratorOptions { Expiry = TimeSpan.FromSeconds(10), Retention = TimeSpan.FromHours(1) });
        (await shortLived.CreateAsync(Args("T", "running"))).IsSuccess.Should().BeTrue();
        var lease = (await shortLived.StartAsync(Key("T", "running"), "worker-a")).Lease!;
        clock.Now = clock.Now.AddSeconds(10);

        var expired = await shortLived.RenewLeaseAsync(lease);
        expired.Outcome.Should().Be(WorkTransitionOutcome.Expired);
        expired.State!.Error!.Code.Should().Be(WorkErrorCodes.OutcomeUnknown);
    }

    [Test]
    public async Task TimeToLive_Covers_Deadline_While_Running_And_Retention_From_Finish()
    {
        var clock = new ManualTimeProvider(_now);
        var provider = new RecordingProvider(new InMemoryWorkProvider(timeProvider: clock));
        var orchestrator = Create(options: new WorkOrchestratorOptions { Expiry = TimeSpan.FromHours(1), Retention = TimeSpan.FromMinutes(10), LeaseDuration = TimeSpan.FromMinutes(1) }, provider: provider);

        var args = Args("T", "abc");
        args.StartTimeout = TimeSpan.FromMinutes(30);
        (await orchestrator.CreateAsync(args)).IsSuccess.Should().BeTrue();
        provider.LastTimeToLive.Should().Be(TimeSpan.FromMinutes(70));

        var lease = (await orchestrator.StartAsync(Key("T", "abc"), "worker-a")).Lease!;
        clock.Now = clock.Now.AddMinutes(5);
        (await orchestrator.CompleteAsync(lease)).IsApplied.Should().BeTrue();
        provider.LastTimeToLive.Should().Be(TimeSpan.FromMinutes(35), "finished work is retained until StartBy plus retention, not ExpiryOn plus retention");

        (await orchestrator.CreateAsync(Args("T", "def"))).IsSuccess.Should().BeTrue();
        (await orchestrator.FenceAsync(Key("T", "def"))).IsApplied.Should().BeTrue();
        provider.LastTimeToLive.Should().Be(TimeSpan.FromMinutes(10));
    }

    [Test]
    public async Task Fence_And_Start_Are_Mutually_Exclusive()
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var orchestrator = Create(new ManualTimeProvider(_now));
            (await orchestrator.CreateAsync(Args("T", $"work-{attempt}"))).IsSuccess.Should().BeTrue();

            var results = await Task.WhenAll(
                Task.Run(() => orchestrator.StartAsync(Key("T", $"work-{attempt}"), "worker-a")),
                Task.Run(() => orchestrator.FenceAsync(Key("T", $"work-{attempt}"))));
            results.Count(result => result.Outcome == WorkTransitionOutcome.Applied).Should().Be(1);
            (await orchestrator.GetAsync(Key("T", $"work-{attempt}")))!.Status.Should().BeOneOf(WorkStatus.Started, WorkStatus.Failed);
        }
    }

    [Test]
    public async Task FailUnowned_Requires_Lease_Expiry()
    {
        var clock = new ManualTimeProvider(_now);
        var orchestrator = Create(clock, new WorkOrchestratorOptions { Expiry = TimeSpan.FromHours(1), LeaseDuration = TimeSpan.FromSeconds(30) });
        (await orchestrator.CreateAsync(Args("T", "abc"))).IsSuccess.Should().BeTrue();
        (await orchestrator.FailUnownedAsync(Key("T", "abc"), new WorkError(WorkErrorCodes.ExecutionFailed, "dead-letter"))).Outcome.Should().Be(WorkTransitionOutcome.Applied);

        (await orchestrator.CreateAsync(Args("T", "def"))).IsSuccess.Should().BeTrue();
        var lease = (await orchestrator.StartAsync(Key("T", "def"), "worker-a")).Lease!;
        (await orchestrator.FailUnownedAsync(Key("T", "def"), new WorkError(WorkErrorCodes.ExecutionFailed, "dead-letter"))).Outcome.Should().Be(WorkTransitionOutcome.LeaseHeld);
        clock.Now = clock.Now.AddSeconds(31);
        (await orchestrator.FailUnownedAsync(Key("T", "def"), new WorkError(WorkErrorCodes.ExecutionFailed, "dead-letter"))).Outcome.Should().Be(WorkTransitionOutcome.Applied);
        (await orchestrator.CompleteAsync(lease)).Outcome.Should().Be(WorkTransitionOutcome.AlreadyFinished);
    }

    [Test]
    public async Task Maximum_Attempts_Fails_On_Takeover()
    {
        var clock = new ManualTimeProvider(_now);
        var orchestrator = Create(clock, new WorkOrchestratorOptions
        {
            Expiry = TimeSpan.FromHours(1),
            LeaseDuration = TimeSpan.FromSeconds(30),
            MaxExecutionAttempts = 1
        });
        (await orchestrator.CreateAsync(Args("T", "abc"))).IsSuccess.Should().BeTrue();
        (await orchestrator.StartAsync(Key("T", "abc"), "worker-a")).Outcome.Should().Be(WorkTransitionOutcome.Applied);
        clock.Now = clock.Now.AddSeconds(31);

        var result = await orchestrator.StartAsync(Key("T", "abc"), "worker-b");
        result.Outcome.Should().Be(WorkTransitionOutcome.Conflict);
        result.State!.Status.Should().Be(WorkStatus.Failed);
        result.State.Error!.Code.Should().Be(WorkErrorCodes.MaxAttempts);
    }

    [Test]
    public async Task Cancel_Is_Immediate_Before_Start_And_Cooperative_During_Execution()
    {
        var orchestrator = Create(new ManualTimeProvider(_now));
        (await orchestrator.CreateAsync(Args("T", "created"))).IsSuccess.Should().BeTrue();
        (await orchestrator.CancelAsync(Key("T", "created"), "not needed")).State!.Status.Should().Be(WorkStatus.Canceled);

        (await orchestrator.CreateAsync(Args("T", "running"))).IsSuccess.Should().BeTrue();
        var lease = (await orchestrator.StartAsync(Key("T", "running"), "worker-a")).Lease!;
        var requested = await orchestrator.CancelAsync(Key("T", "running"), "stop");
        requested.State!.Status.Should().Be(WorkStatus.Started);
        requested.State.CancelRequested.Should().BeTrue();
        (await orchestrator.AcknowledgeCancelAsync(lease)).State!.Status.Should().Be(WorkStatus.Canceled);
    }

    [Test]
    public async Task Ownership_Is_Fail_Closed_And_Anonymous_Creation_Is_Unowned()
    {
        var orchestrator = Create(new ManualTimeProvider(_now));
        (await orchestrator.CreateAsync(Args("T", "owned"))).IsSuccess.Should().BeTrue();
        (await orchestrator.GetWithTypeAsync("T", Key("T", "owned"))).Should().NotBeNull();

        ExecutionContext.Current.User = new AuthenticationUser { Type = AuthenticationType.AccountUser, UserName = "alice", Id = "456" };
        (await orchestrator.GetWithTypeAsync("T", Key("T", "owned"))).Should().BeNull();
        ExecutionContext.Reset();
        (await orchestrator.GetWithTypeAsync("T", Key("T", "owned"))).Should().BeNull();

        ExecutionContext.Current.User = AuthenticationUser.Anonymous;
        (await orchestrator.CreateAsync(Args("T", "anonymous"))).IsSuccess.Should().BeTrue();
        (await orchestrator.GetWithTypeAsync("T", Key("T", "anonymous"))).Should().NotBeNull();
        ExecutionContext.Current.User = _user;
        (await orchestrator.GetWithTypeAsync("T", Key("T", "anonymous"))).Should().NotBeNull();
    }

    [Test]
    public async Task Missing_Execution_Context_Uses_Environment_User_As_Owner()
    {
        ExecutionContext.Reset();
        var orchestrator = new WorkOrchestrator(new InMemoryWorkProvider(timeProvider: new ManualTimeProvider(_now)));
        var created = await orchestrator.CreateAsync(Args("T", "system-created"));
        created.IsSuccess.Should().BeTrue();
        created.Value.User.Should().Be(AuthenticationUser.EnvironmentUser);
        (await orchestrator.GetAsync(Key("T", "system-created")))!.User.Should().Be(AuthenticationUser.EnvironmentUser);

        ExecutionContext.Reset();
        (await orchestrator.GetWithTypeAsync("T", Key("T", "system-created"))).Should().BeNull();
        ExecutionContext.Current.User = AuthenticationUser.Anonymous;
        (await orchestrator.GetWithTypeAsync("T", Key("T", "system-created"))).Should().BeNull();
        ExecutionContext.Current.User = AuthenticationUser.EnvironmentUser;
        (await orchestrator.GetWithTypeAsync("T", Key("T", "system-created"))).Should().NotBeNull();
    }

    [Test]
    public async Task Explicit_Owner_Takes_Precedence_Without_Execution_Context()
    {
        ExecutionContext.Reset();
        var orchestrator = new WorkOrchestrator(new InMemoryWorkProvider(timeProvider: new ManualTimeProvider(_now)));
        var args = Args("T", "explicit-owner");
        args.User = _user;
        var created = await orchestrator.CreateAsync(args);

        created.IsSuccess.Should().BeTrue();
        created.Value.User.Should().Be(_user);
        (await orchestrator.GetAsync(Key("T", "explicit-owner")))!.User.Should().Be(_user);
    }

    [Test]
    public async Task ResourceLocation_Rejects_External_Or_Invalid_Uris_Before_Writing()
    {
        var orchestrator = Create(new ManualTimeProvider(_now));
        (await orchestrator.CreateAsync(Args("T", "abc"))).IsSuccess.Should().BeTrue();
        var lease = (await orchestrator.StartAsync(Key("T", "abc"), "worker-a")).Lease!;

        foreach (var value in new[] { "https://evil.example/x", "//evil.example/x", "/\\evil", "products/123", "/bad\r\nheader", "/" + new string('x', 2048) })
        {
            var action = () => orchestrator.CompleteAsync(lease, value);
            await FluentActions.Awaiting(action).Should().ThrowAsync<ArgumentException>();
            (await orchestrator.GetAsync(Key("T", "abc")))!.Status.Should().Be(WorkStatus.Started);
        }

        (await orchestrator.CompleteAsync(lease, "/products/123")).Outcome.Should().Be(WorkTransitionOutcome.Applied);
    }

    [Test]
    public async Task Duplicate_Create_And_Invalid_Transitions_Are_Explicit()
    {
        var orchestrator = Create(new ManualTimeProvider(_now));
        (await orchestrator.StartAsync(Key("T", "missing"), "worker-a")).Outcome.Should().Be(WorkTransitionOutcome.NotFound);
        (await orchestrator.CreateAsync(Args("T", "abc"))).IsSuccess.Should().BeTrue();
        (await orchestrator.CreateAsync(Args("T", "abc"))).IsFailureOfType<ConflictException>().Should().BeTrue();
        (await orchestrator.CompleteAsync(new WorkLease(Key("T", "abc"), "worker-a", 1, _now))).Outcome.Should().Be(WorkTransitionOutcome.Conflict);
        (await orchestrator.StartAsync(Key("T", "abc"), "worker-a")).Outcome.Should().Be(WorkTransitionOutcome.Applied);
        (await orchestrator.StartAsync(Key("T", "abc"), "worker-b")).Outcome.Should().Be(WorkTransitionOutcome.LeaseHeld);
        (await orchestrator.DeleteAsync(Key("T", "abc"))).IsFailureOfType<ConflictException>().Should().BeTrue();
    }

    [Test]
    public async Task Ambiguous_Timeout_Is_Resolved_Only_When_Revision_Is_Observed()
    {
        var appliedProvider = new AmbiguousProvider(new InMemoryWorkProvider(timeProvider: new ManualTimeProvider(_now)), true);
        var applied = Create(provider: appliedProvider);
        (await applied.CreateAsync(Args("T", "applied"))).IsSuccess.Should().BeTrue();
        (await applied.StartAsync(Key("T", "applied"), "worker-a")).Outcome.Should().Be(WorkTransitionOutcome.Applied);

        var missingProvider = new AmbiguousProvider(new InMemoryWorkProvider(timeProvider: new ManualTimeProvider(_now)), false);
        var missing = Create(provider: missingProvider);
        (await missing.CreateAsync(Args("T", "missing"))).IsSuccess.Should().BeTrue();
        var start = () => missing.StartAsync(Key("T", "missing"), "worker-a");
        await FluentActions.Awaiting(start).Should().ThrowAsync<WorkOutcomeUnknownException>();
    }

    [Test]
    public async Task Get_Returns_Independent_Copy_And_Delete_Removes_Finished_State()
    {
        var orchestrator = Create(new ManualTimeProvider(_now));
        (await orchestrator.CreateAsync(Args("T", "abc"))).IsSuccess.Should().BeTrue();
        var state = await orchestrator.GetAsync(Key("T", "abc"));
        state!.Status = WorkStatus.Completed;
        (await orchestrator.GetAsync(Key("T", "abc")))!.Status.Should().Be(WorkStatus.Created);

        var lease = (await orchestrator.StartAsync(Key("T", "abc"), "worker-a")).Lease!;
        var first = await orchestrator.CompleteAsync(lease);
        var second = await orchestrator.CompleteAsync(lease);
        first.Outcome.Should().Be(WorkTransitionOutcome.Applied);
        second.Outcome.Should().Be(WorkTransitionOutcome.AlreadyFinished);
        (await orchestrator.DeleteAsync(Key("T", "abc"))).IsSuccess.Should().BeTrue();
        (await orchestrator.GetAsync(Key("T", "abc"))).Should().BeNull();
        (await orchestrator.DeleteAsync(Key("T", "abc"))).IsSuccess.Should().BeTrue();
    }

    [Test]
    public void WorkTransitionResult_ToResult_Sets_Distinct_ErrorCodes()
    {
        var held = new WorkTransitionResult(WorkTransitionOutcome.LeaseHeld, null).ToResult();
        held.Error.Should().BeOfType<ConflictException>().Which.ErrorCode.Should().Be(WorkTransitionErrorCodes.LeaseHeld);

        var expired = new WorkTransitionResult(WorkTransitionOutcome.Expired, null).ToResult();
        expired.Error.Should().BeOfType<ConflictException>().Which.ErrorCode.Should().Be(WorkTransitionErrorCodes.Expired);

        var missing = new WorkTransitionResult(WorkTransitionOutcome.NotFound, null).ToResult();
        missing.Error.Should().BeOfType<NotFoundException>().Which.ErrorCode.Should().Be(WorkTransitionErrorCodes.NotFound);
    }

    [Test]
    public void WorkState_Identity_And_Timestamps_Use_Expected_Json_Fields()
    {
        var key = Key("T", "timestamp");
        var state = new WorkState
        {
            Scope = key.Scope,
            DomainName = key.DomainName,
            TenantId = key.TenantId,
            OperationName = key.OperationName,
            Id = "timestamp",
            CreatedOn = _now,
            ExpiryOn = _now.AddDays(1),
            StartedOn = _now.AddMinutes(1),
            IndeterminateOn = _now.AddMinutes(2),
            FinishedOn = _now.AddMinutes(3)
        };
        var json = JsonSerializer.SerializeToElement(state, JsonDefaults.SerializerOptions);
        json.TryGetProperty("operationKey", out _).Should().BeFalse();
        json.GetProperty("scope").GetString().Should().Be(key.Scope.ToString());
        json.GetProperty("domainName").GetString().Should().Be(key.DomainName);
        json.GetProperty("operationName").GetString().Should().Be(key.OperationName);
        json.GetProperty("created").GetDateTimeOffset().Should().Be(state.CreatedOn);
        json.GetProperty("expiry").GetDateTimeOffset().Should().Be(state.ExpiryOn);
        json.GetProperty("started").GetDateTimeOffset().Should().Be(state.StartedOn);
        json.GetProperty("indeterminate").GetDateTimeOffset().Should().Be(state.IndeterminateOn);
        json.GetProperty("finished").GetDateTimeOffset().Should().Be(state.FinishedOn);
    }
}
