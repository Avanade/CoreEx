namespace CoreEx.Test.Unit.Operations;

/// <summary>
/// Provides the provider-agnostic <see cref="IOperationStore"/> behavior suite run against every provider.
/// </summary>
/// <remarks>Tests use unique instance identifiers and only assert on their own records so they can run against shared relational tables.</remarks>
public abstract class OperationStoreTestsBase
{
    /// <summary>Gets the claim duration used by the suite.</summary>
    protected static readonly TimeSpan ClaimDuration = TimeSpan.FromSeconds(2);

    /// <summary>Gets the delay that reliably passes a claim expiry.</summary>
    protected static readonly TimeSpan PastClaim = ClaimDuration + TimeSpan.FromMilliseconds(500);

    /// <summary>Gets the suite options with short retention windows.</summary>
    /// <remarks>The inactivity timeout stays long so cleanup never expires other tests' rows in shared tables; abandonment tests use <see cref="AbandonOptions"/>.</remarks>
    protected static OperationStoreOptions SuiteOptions() => new() { TerminalRetention = TimeSpan.FromSeconds(2), InactivityTimeout = TimeSpan.FromHours(1) };

    /// <summary>Gets the options with a short inactivity timeout for abandonment tests.</summary>
    protected static OperationStoreOptions AbandonOptions() => new() { TerminalRetention = TimeSpan.FromSeconds(2), InactivityTimeout = TimeSpan.FromSeconds(2) };

    /// <summary>Gets the delay that reliably passes the abandonment inactivity timeout.</summary>
    protected static readonly TimeSpan PastInactivity = TimeSpan.FromSeconds(2) + TimeSpan.FromMilliseconds(500);

    /// <summary>Creates the store under test.</summary>
    protected abstract OperationStoreBase CreateStore(OperationStoreOptions options);

    /// <summary>Gets a store sharing the <paramref name="store"/> state for use by a concurrent caller; relational providers return a new scope as a single connection cannot be used concurrently.</summary>
    protected virtual OperationStoreBase CreateConcurrentStore(OperationStoreBase store) => store;

    /// <summary>Gets the log entries captured by loggers from <see cref="CreateLogger{T}"/> during the current test.</summary>
    protected List<(LogLevel Level, string Message)> LogEntries { get; } = [];

    /// <summary>Gets the operation name used by the suite.</summary>
    protected const string OperationName = "op";

    /// <summary>Gets the tenant used by the suite.</summary>
    protected const string TenantId = "tenant-a";

    /// <summary>Gets the host settings used by the suite.</summary>
    protected static readonly IHostSettings Settings = new HostSettings { SolutionName = "Tests", DomainName = "CoreEx.Test", EnvironmentName = "Test" };

    /// <summary>Clears the captured log entries and sets the ambient tenant before each test.</summary>
    [SetUp]
    public void ClearLogEntries()
    {
        LogEntries.Clear();
        ExecutionContext.Reset();
        ExecutionContext.SetCurrent(new ExecutionContext { TenantId = TenantId });
    }

    /// <summary>Resets the ambient execution context after each test.</summary>
    [TearDown]
    public void ResetExecutionContext() => ExecutionContext.Reset();

    /// <summary>Creates a logger that captures its entries into <see cref="LogEntries"/>.</summary>
    protected ILogger<T> CreateLogger<T>() => new CapturingLogger<T>(LogEntries);

    /// <summary>Captures formatted log entries for assertion.</summary>
    private sealed class CapturingLogger<T>(List<(LogLevel Level, string Message)> entries) : ILogger<T>
    {
        /// <inheritdoc/>
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        /// <inheritdoc/>
        public bool IsEnabled(LogLevel logLevel) => true;

        /// <inheritdoc/>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            lock (entries)
            {
                entries.Add((logLevel, formatter(state, exception)));
            }
        }
    }

    /// <summary>Advances store time by at least <paramref name="duration"/>.</summary>
    protected abstract Task AdvanceAsync(TimeSpan duration);

    /// <summary>Creates a unique operation key.</summary>
    protected static CompositeKey NewKey(string? instanceId = null) => CompositeKey.Create(instanceId ?? Guid.NewGuid().ToString());

    /// <summary>Cleans up until neither the expire nor the purge batch is full.</summary>
    protected static async Task CleanupAllAsync(OperationStoreBase store)
    {
        OperationCleanupResult result;
        do
        {
            result = await store.CleanupAsync(100);
        } while (result.Expired == 100 || result.Purged == 100);
    }

    /// <summary>Creates and claims a new operation.</summary>
    protected static async Task<OperationClaim> CreateAndClaimAsync(OperationStoreBase store, string owner = "worker-a")
    {
        var created = await store.CreateAsync(OperationName, NewKey());
        var result = await store.ClaimAsync(created.State.OperationId, owner, ClaimDuration);
        result.Outcome.Should().Be(OperationClaimOutcome.Claimed);
        return result.Claim!;
    }

    [Test]
    public async Task Create_Then_Get_By_Id_And_Key()
    {
        var store = CreateStore(SuiteOptions());
        var key = NewKey();

        var created = await store.CreateAsync(OperationName, key, "user-1", "{\"a\":1}");

        created.Created.Should().BeTrue();
        var state = created.State;
        state.DomainName.Should().Be("CoreEx.Test");
        state.TenantId.Should().Be(TenantId);
        state.OperationName.Should().Be(OperationName);
        state.InstanceId.Should().Be(key.ToCanonicalString());
        state.OperationId.Should().NotBeEmpty();
        state.Status.Should().Be(OperationStatus.Accepted);
        state.InitiatedBy.Should().Be("user-1");
        state.Data.Should().Be("{\"a\":1}");
        state.Attempt.Should().Be(0);
        state.ClaimOwner.Should().BeNull();
        state.StartedOn.Should().BeNull();
        state.CompletedOn.Should().BeNull();
        state.PurgeAfter.Should().BeNull();
        state.UpdatedOn.Should().Be(state.CreatedOn);
        state.ETag.Should().NotBeNullOrEmpty();

        (await store.GetByIdAsync(state.OperationId)).Should().Be(state);
        (await store.GetByKeyAsync(OperationName, key)).Should().Be(state);
        (await store.GetByIdAsync(Guid.NewGuid())).Should().BeNull();
        (await store.GetByKeyAsync(OperationName, NewKey())).Should().BeNull();
    }

    [Test]
    public async Task Create_Duplicate_Returns_Existing_Without_Replacing()
    {
        var store = CreateStore(SuiteOptions());
        var key = NewKey();
        var first = await store.CreateAsync(OperationName, key, "user-1");

        var second = await store.CreateAsync(OperationName, key, "user-2", "{}");

        second.Created.Should().BeFalse();
        second.State.Should().Be(first.State);
    }

    [Test]
    public async Task LifetimeOverrides_AreCapturedAndAppliedAcrossTransitions()
    {
        var defaults = SuiteOptions();
        var store = CreateStore(defaults);
        var key = NewKey();
        var inactivity = TimeSpan.FromSeconds(8);
        var retention = TimeSpan.FromSeconds(9);
        var options = new OperationCreateOptions { InactivityTimeout = inactivity, TerminalRetention = retention };
        var created = await store.CreateAsync(OperationName, key, "owner", null, options);
        created.State.InactivityTimeout.Should().Be(inactivity);
        created.State.TerminalRetention.Should().Be(retention);
        (created.State.AbandonAfter - created.State.CreatedOn).Should().Be(inactivity);

        defaults.InactivityTimeout = TimeSpan.FromHours(2);
        defaults.TerminalRetention = TimeSpan.FromHours(3);
        var other = CreateConcurrentStore(store);
        var duplicate = await other.CreateAsync(OperationName, key, "owner", null, new OperationCreateOptions { InactivityTimeout = TimeSpan.FromSeconds(1), TerminalRetention = TimeSpan.FromSeconds(1) });
        duplicate.Created.Should().BeFalse();
        duplicate.State.InactivityTimeout.Should().Be(inactivity);
        duplicate.State.TerminalRetention.Should().Be(retention);

        var claimResult = await other.ClaimAsync(created.State.OperationId, "worker", ClaimDuration);
        var claim = claimResult.Claim.ThrowIfNull();
        (claimResult.State!.AbandonAfter - claimResult.State.ClaimExpiresOn).Should().Be(inactivity);
        claim = (await other.RenewAsync(claim, ClaimDuration)).ThrowIfNull();
        var renewed = (await other.GetByIdAsync(created.State.OperationId)).ThrowIfNull();
        (renewed.AbandonAfter - renewed.ClaimExpiresOn).Should().Be(inactivity);
        (await other.ReleaseAsync(claim)).Should().BeTrue();
        var released = (await other.GetByIdAsync(created.State.OperationId)).ThrowIfNull();
        (released.AbandonAfter - released.UpdatedOn).Should().Be(inactivity);
        claim = (await other.ClaimAsync(created.State.OperationId, "worker", ClaimDuration)).Claim.ThrowIfNull();
        var complete = (await other.CompleteAsync(claim, OperationCompletion.Succeeded())).ThrowIfNull();
        (complete.PurgeAfter - complete.CompletedOn).Should().Be(retention);

        var cancelled = await store.CreateAsync(OperationName, NewKey(), "owner", null, options);
        var cancelResult = (await other.RequestCancelAsync(cancelled.State.OperationId)).ThrowIfNull();
        (cancelResult.PurgeAfter - cancelResult.CompletedOn).Should().Be(retention);
    }

    [Test]
    public async Task LifetimeOverrides_CleanupUsesEachOperationsRetention()
    {
        var store = CreateStore(SuiteOptions());
        var first = await store.CreateAsync(OperationName, NewKey(), null, null, new OperationCreateOptions { InactivityTimeout = TimeSpan.FromMilliseconds(100), TerminalRetention = TimeSpan.FromSeconds(8) });
        var second = await store.CreateAsync(OperationName, NewKey(), null, null, new OperationCreateOptions { InactivityTimeout = TimeSpan.FromMilliseconds(100), TerminalRetention = TimeSpan.FromSeconds(9) });
        await AdvanceAsync(TimeSpan.FromMilliseconds(300));
        var other = CreateConcurrentStore(store);
        await CleanupAllAsync(other);
        var expiredFirst = (await other.GetByIdAsync(first.State.OperationId)).ThrowIfNull();
        var expiredSecond = (await other.GetByIdAsync(second.State.OperationId)).ThrowIfNull();
        expiredFirst.Status.Should().Be(OperationStatus.Expired);
        expiredSecond.Status.Should().Be(OperationStatus.Expired);
        (expiredFirst.PurgeAfter - expiredFirst.CompletedOn).Should().Be(TimeSpan.FromSeconds(8));
        (expiredSecond.PurgeAfter - expiredSecond.CompletedOn).Should().Be(TimeSpan.FromSeconds(9));
    }

    [Test]
    public async Task LifetimeOverrides_ValidateAndRoundUpToMilliseconds()
    {
        var store = CreateStore(SuiteOptions());
        await FluentActions.Awaiting(() => store.CreateAsync(OperationName, NewKey(), null, null, new OperationCreateOptions { InactivityTimeout = TimeSpan.Zero }))
            .Should().ThrowAsync<ArgumentOutOfRangeException>().WithParameterName("InactivityTimeout");
        await FluentActions.Awaiting(() => store.CreateAsync(OperationName, NewKey(), null, null, new OperationCreateOptions { TerminalRetention = TimeSpan.MaxValue }))
            .Should().ThrowAsync<ArgumentOutOfRangeException>().WithParameterName("TerminalRetention");
        var created = await store.CreateAsync(OperationName, NewKey(), null, null, new OperationCreateOptions { InactivityTimeout = TimeSpan.FromTicks(1), TerminalRetention = TimeSpan.FromTicks(10001) });
        created.State.InactivityTimeout.Should().Be(TimeSpan.FromMilliseconds(1));
        created.State.TerminalRetention.Should().Be(TimeSpan.FromMilliseconds(2));
        var partial = await store.CreateAsync(OperationName, NewKey(), null, null, new OperationCreateOptions { InactivityTimeout = TimeSpan.FromSeconds(4) });
        partial.State.InactivityTimeout.Should().Be(TimeSpan.FromSeconds(4));
        partial.State.TerminalRetention.Should().Be(SuiteOptions().TerminalRetention);
    }

    [Test]
    public async Task Create_CanonicalCompositeIdentity_UsesValueBasedEquality()
    {
        var store = CreateStore(SuiteOptions());
        var id = Guid.NewGuid().ToString();
        var numeric = CompositeKey.Create(id, 123);
        var textual = CompositeKey.Create(id, "123");

        var first = await store.CreateAsync(OperationName, numeric);
        var second = await store.CreateAsync(OperationName, textual);

        first.Created.Should().BeTrue();
        second.Created.Should().BeFalse();
        second.State.OperationId.Should().Be(first.State.OperationId);
        (await store.GetByKeyAsync(OperationName, textual))!.OperationId.Should().Be(first.State.OperationId);
        (await store.GetByKeyAsync(OperationName, CompositeKey.Create(numeric.ToCanonicalString())))!.OperationId.Should().Be(first.State.OperationId);
    }

    [TestCase("")]
    [TestCase(" ")]
    public async Task Create_And_Get_Reject_Blank_InstanceKey(string value)
    {
        var store = CreateStore(SuiteOptions());
        var key = CompositeKey.Create(value);

        await FluentActions.Awaiting(() => store.CreateAsync(OperationName, key)).Should().ThrowAsync<ArgumentException>().WithParameterName("key");
        await FluentActions.Awaiting(() => store.GetByKeyAsync(OperationName, key)).Should().ThrowAsync<ArgumentException>().WithParameterName("key");
    }

    [Test]
    public async Task Create_Identity_Preserves_Tenant_And_Component_Boundaries()
    {
        var store = CreateStore(SuiteOptions());
        var id = Guid.NewGuid().ToString();
        var key = NewKey(id);

        ExecutionContext.Current.TenantId = null;
        var domain = await store.CreateAsync(OperationName, key);
        ExecutionContext.Current.TenantId = "-";
        var tenant = await store.CreateAsync(OperationName, key);
        var first = await store.CreateAsync($"import~{id}", NewKey("end"));
        var second = await store.CreateAsync("import", NewKey($"{id}~end"));

        new[] { domain, tenant, first, second }.Should().AllSatisfy(x => x.Created.Should().BeTrue());
        new[] { domain, tenant, first, second }.Select(x => x.State.OperationId).Should().OnlyHaveUniqueItems();
        (await store.GetByKeyAsync(OperationName, key))!.OperationId.Should().Be(tenant.State.OperationId);
        (await store.GetByKeyAsync($"import~{id}", NewKey("end")))!.OperationId.Should().Be(first.State.OperationId);
        (await store.GetByKeyAsync("import", NewKey($"{id}~end")))!.OperationId.Should().Be(second.State.OperationId);
        ExecutionContext.Current.TenantId = null;
        (await store.GetByKeyAsync(OperationName, key))!.OperationId.Should().Be(domain.State.OperationId);
    }

    [Test]
    public async Task Create_Identity_Is_Ordinal()
    {
        var store = CreateStore(SuiteOptions());
        var id = Guid.NewGuid().ToString();

        var ids = new[] { await store.CreateAsync(OperationName, NewKey($"a{id}")), await store.CreateAsync(OperationName, NewKey($"A{id}")), await store.CreateAsync(OperationName, NewKey($"a{id} ")) };

        ids.Should().AllSatisfy(x => x.Created.Should().BeTrue());
        ids.Select(x => x.State.OperationId).Should().OnlyHaveUniqueItems();
        (await store.GetByKeyAsync(OperationName, NewKey($"A{id}")))!.OperationId.Should().Be(ids[1].State.OperationId);
    }

    [Test]
    public async Task Create_Concurrent_Creates_Exactly_One()
    {
        var store = CreateStore(SuiteOptions());
        var key = NewKey();

        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() => CreateConcurrentStore(store).CreateAsync(OperationName, key))));

        results.Count(x => x.Created).Should().Be(1);
        results.Select(x => x.State.OperationId).Distinct().Should().ContainSingle();
    }

    [Test]
    public async Task Claim_Then_Busy_Then_NotFound()
    {
        var store = CreateStore(SuiteOptions());
        var created = await store.CreateAsync(OperationName, NewKey());

        var claimed = await store.ClaimAsync(created.State.OperationId, "worker-a", ClaimDuration);

        claimed.Outcome.Should().Be(OperationClaimOutcome.Claimed);
        claimed.State!.Status.Should().Be(OperationStatus.Running);
        claimed.State.Attempt.Should().Be(1);
        claimed.State.ClaimOwner.Should().Be("worker-a");
        claimed.State.StartedOn.Should().Be(claimed.State.UpdatedOn);
        claimed.State.ClaimExpiresOn.Should().BeCloseTo(claimed.State.UpdatedOn + ClaimDuration, TimeSpan.FromMilliseconds(1));
        claimed.Claim.Should().Be(new OperationClaim(created.State.OperationId, "worker-a", 1, claimed.State.ClaimExpiresOn!.Value, false));

        var busy = await store.ClaimAsync(created.State.OperationId, "worker-b", ClaimDuration);
        busy.Outcome.Should().Be(OperationClaimOutcome.Busy);
        busy.Claim.Should().BeNull();
        busy.State.Should().Be(claimed.State);

        var missing = await store.ClaimAsync(Guid.NewGuid(), "worker-a", ClaimDuration);
        missing.Should().Be(new OperationClaimResult(OperationClaimOutcome.NotFound, null, null));
    }

    [Test]
    public async Task Claim_Concurrent_Claims_Exactly_One()
    {
        var store = CreateStore(SuiteOptions());
        var created = await store.CreateAsync(OperationName, NewKey());

        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(i => Task.Run(() => CreateConcurrentStore(store).ClaimAsync(created.State.OperationId, $"worker-{i}", ClaimDuration))));

        results.Count(x => x.Outcome == OperationClaimOutcome.Claimed).Should().Be(1);
        results.Count(x => x.Outcome == OperationClaimOutcome.Busy).Should().Be(7);
        (await store.GetByIdAsync(created.State.OperationId))!.Attempt.Should().Be(1);
    }

    [Test]
    public async Task Renew_Extends_Claim_And_Records_Progress_Until_Lost()
    {
        var store = CreateStore(SuiteOptions());
        var claim = await CreateAndClaimAsync(store);

        await AdvanceAsync(TimeSpan.FromMilliseconds(500));
        var renewed = await store.RenewAsync(claim, ClaimDuration, "50%");

        renewed.Should().NotBeNull();
        renewed!.Attempt.Should().Be(claim.Attempt);
        renewed.ExpiresOn.Should().BeAfter(claim.ExpiresOn);
        var state = await store.GetByIdAsync(claim.OperationId);
        state!.Progress.Should().Be("50%");
        state.ClaimExpiresOn.Should().Be(renewed.ExpiresOn);

        (await store.RenewAsync(renewed, ClaimDuration)).Should().NotBeNull();
        (await store.GetByIdAsync(claim.OperationId))!.Progress.Should().Be("50%");

        await AdvanceAsync(PastClaim);
        (await store.RenewAsync(renewed, ClaimDuration)).Should().BeNull();
    }

    [Test]
    public async Task Takeover_After_Expiry_Fences_The_Previous_Owner()
    {
        var store = CreateStore(SuiteOptions());
        var first = await CreateAndClaimAsync(store, "worker-a");
        var startedOn = (await store.GetByIdAsync(first.OperationId))!.StartedOn;

        await AdvanceAsync(PastClaim);
        var takeover = await store.ClaimAsync(first.OperationId, "worker-b", ClaimDuration);

        takeover.Outcome.Should().Be(OperationClaimOutcome.Claimed);
        takeover.Claim!.Attempt.Should().Be(2);
        takeover.State!.StartedOn.Should().Be(startedOn);
        (await store.RenewAsync(first, ClaimDuration)).Should().BeNull();
        (await store.ReleaseAsync(first)).Should().BeFalse();
        (await store.CompleteAsync(first, OperationCompletion.Succeeded())).Should().BeNull();
        (await store.CompleteAsync(takeover.Claim, OperationCompletion.Succeeded())).Should().NotBeNull();
    }

    [Test]
    public async Task Takeover_Fences_A_Same_Owner_Earlier_Attempt()
    {
        var store = CreateStore(SuiteOptions());
        var first = await CreateAndClaimAsync(store, "worker-a");

        await AdvanceAsync(PastClaim);
        var second = (await store.ClaimAsync(first.OperationId, "worker-a", ClaimDuration)).Claim!;

        (await store.CompleteAsync(first, OperationCompletion.Succeeded())).Should().BeNull();
        (await store.CompleteAsync(second, OperationCompletion.Succeeded())).Should().NotBeNull();
    }

    [Test]
    public async Task Complete_Succeeded_Is_Fenced_And_Final()
    {
        var store = CreateStore(SuiteOptions());
        var claim = await CreateAndClaimAsync(store);

        var completed = await store.CompleteAsync(claim, OperationCompletion.Succeeded("/orders/1"));

        completed.Should().NotBeNull();
        completed!.Status.Should().Be(OperationStatus.Succeeded);
        completed.IsFinished.Should().BeTrue();
        completed.ResultLocation.Should().Be("/orders/1");
        completed.ClaimOwner.Should().BeNull();
        completed.ClaimExpiresOn.Should().BeNull();
        completed.CompletedOn.Should().Be(completed.UpdatedOn);
        completed.PurgeAfter.Should().BeCloseTo(completed.CompletedOn!.Value + SuiteOptions().TerminalRetention, TimeSpan.FromMilliseconds(1));
        (await store.GetByIdAsync(claim.OperationId)).Should().Be(completed);

        (await store.CompleteAsync(claim, OperationCompletion.Failed("late"))).Should().BeNull();
        (await store.ClaimAsync(claim.OperationId, "worker-b", ClaimDuration)).Should().Be(new OperationClaimResult(OperationClaimOutcome.Finished, completed, null));
    }

    [Test]
    public async Task Complete_Failed_Records_Error()
    {
        var store = CreateStore(SuiteOptions());
        var claim = await CreateAndClaimAsync(store);

        var completed = await store.CompleteAsync(claim, OperationCompletion.Failed("E42", "It broke."));

        completed!.Status.Should().Be(OperationStatus.Failed);
        completed.ErrorCode.Should().Be("E42");
        completed.ErrorMessage.Should().Be("It broke.");
        completed.ResultLocation.Should().BeNull();
    }

    [Test]
    public async Task Complete_After_Expiry_Fails_Without_Takeover()
    {
        var store = CreateStore(SuiteOptions());
        var claim = await CreateAndClaimAsync(store);

        await AdvanceAsync(PastClaim);

        (await store.CompleteAsync(claim, OperationCompletion.Succeeded())).Should().BeNull();
        (await store.GetByIdAsync(claim.OperationId))!.Status.Should().Be(OperationStatus.Running);
    }

    [Test]
    public async Task Release_Allows_Immediate_Reclaim()
    {
        var store = CreateStore(SuiteOptions());
        var claim = await CreateAndClaimAsync(store);

        (await store.ReleaseAsync(claim, "TRANSIENT", "Downstream unavailable.")).Should().BeTrue();
        var released = await store.GetByIdAsync(claim.OperationId);
        released!.Status.Should().Be(OperationStatus.Retrying);
        released.IsFinished.Should().BeFalse();
        released.ErrorCode.Should().Be("TRANSIENT");
        released.ErrorMessage.Should().Be("Downstream unavailable.");
        released.ClaimOwner.Should().BeNull();
        released.ClaimExpiresOn.Should().BeNull();
        (await store.ReleaseAsync(claim)).Should().BeFalse();
        (await store.CompleteAsync(claim, OperationCompletion.Succeeded())).Should().BeNull();

        var reclaimed = await store.ClaimAsync(claim.OperationId, "worker-b", ClaimDuration);
        reclaimed.Outcome.Should().Be(OperationClaimOutcome.Claimed);
        reclaimed.Claim!.Attempt.Should().Be(2);
        reclaimed.State!.Status.Should().Be(OperationStatus.Running);
        reclaimed.State.ErrorCode.Should().Be("TRANSIENT");

        var completed = await store.CompleteAsync(reclaimed.Claim, OperationCompletion.Succeeded());
        completed!.ErrorCode.Should().BeNull();
        completed.ErrorMessage.Should().BeNull();
    }

    [Test]
    public async Task Release_Without_Error_Clears_Previous_Error()
    {
        var store = CreateStore(SuiteOptions());
        var first = await CreateAndClaimAsync(store);
        (await store.ReleaseAsync(first, "TRANSIENT", "First.")).Should().BeTrue();
        var second = (await store.ClaimAsync(first.OperationId, "worker-b", ClaimDuration)).Claim!;

        (await store.ReleaseAsync(second)).Should().BeTrue();
        var released = await store.GetByIdAsync(first.OperationId);
        released!.Status.Should().Be(OperationStatus.Retrying);
        released.ErrorCode.Should().BeNull();
        released.ErrorMessage.Should().BeNull();
    }

    [Test]
    public async Task Cancel_Retrying_Finishes_Immediately()
    {
        var store = CreateStore(SuiteOptions());
        var claim = await CreateAndClaimAsync(store);
        (await store.ReleaseAsync(claim, "TRANSIENT", "Downstream unavailable.")).Should().BeTrue();

        var cancelled = await store.RequestCancelAsync(claim.OperationId);

        cancelled!.Status.Should().Be(OperationStatus.Cancelled);
        cancelled.ErrorCode.Should().BeNull();
        cancelled.ErrorMessage.Should().BeNull();
        cancelled.CompletedOn.Should().NotBeNull();
        (await store.ClaimAsync(claim.OperationId, "worker-b", ClaimDuration)).Outcome.Should().Be(OperationClaimOutcome.Finished);
    }

    [Test]
    public async Task Cancel_Accepted_Finishes_Immediately()
    {
        var store = CreateStore(SuiteOptions());
        var created = await store.CreateAsync(OperationName, NewKey());

        var cancelled = await store.RequestCancelAsync(created.State.OperationId);

        cancelled!.Status.Should().Be(OperationStatus.Cancelled);
        cancelled.CancelRequestedOn.Should().NotBeNull();
        cancelled.CompletedOn.Should().NotBeNull();
        cancelled.PurgeAfter.Should().NotBeNull();
        (await store.ClaimAsync(created.State.OperationId, "worker-a", ClaimDuration)).Outcome.Should().Be(OperationClaimOutcome.Finished);
        (await store.RequestCancelAsync(created.State.OperationId)).Should().Be(cancelled);
    }

    [Test]
    public async Task Cancel_Running_Is_Cooperative()
    {
        var store = CreateStore(SuiteOptions());
        var claim = await CreateAndClaimAsync(store);

        var requested = await store.RequestCancelAsync(claim.OperationId);

        requested!.Status.Should().Be(OperationStatus.Running);
        requested.CancelRequestedOn.Should().NotBeNull();
        (await store.RequestCancelAsync(claim.OperationId)).Should().Be(requested);

        var renewed = await store.RenewAsync(claim, ClaimDuration);
        renewed!.CancelRequested.Should().BeTrue();

        var completed = await store.CompleteAsync(renewed, OperationCompletion.Cancelled());
        completed!.Status.Should().Be(OperationStatus.Cancelled);
        completed.CancelRequestedOn.Should().Be(requested.CancelRequestedOn);
    }

    [Test]
    public async Task Cancel_Finished_Or_Unknown_Is_Unchanged()
    {
        var store = CreateStore(SuiteOptions());
        var claim = await CreateAndClaimAsync(store);
        var completed = await store.CompleteAsync(claim, OperationCompletion.Succeeded());

        (await store.RequestCancelAsync(claim.OperationId)).Should().Be(completed);
        (await store.RequestCancelAsync(Guid.NewGuid())).Should().BeNull();
    }

    [Test]
    public async Task ETag_Changes_On_Every_Mutation_And_Recreation()
    {
        var store = CreateStore(SuiteOptions());
        var key = NewKey();
        var etags = new List<string>();
        var ids = new List<Guid>();

        var created = await store.CreateAsync(OperationName, key);
        etags.Add(created.State.ETag);
        ids.Add(created.State.OperationId);
        var claim = (await store.ClaimAsync(created.State.OperationId, "worker-a", ClaimDuration)).Claim!;
        etags.Add((await store.GetByKeyAsync(OperationName, key))!.ETag);
        claim = (await store.RenewAsync(claim, ClaimDuration, "p"))!;
        etags.Add((await store.GetByKeyAsync(OperationName, key))!.ETag);
        await store.RequestCancelAsync(claim.OperationId);
        etags.Add((await store.GetByKeyAsync(OperationName, key))!.ETag);
        etags.Add((await store.CompleteAsync(claim, OperationCompletion.Cancelled()))!.ETag);

        await AdvanceAsync(SuiteOptions().TerminalRetention + TimeSpan.FromMilliseconds(500));
        await CleanupAllAsync(store);
        (await store.GetByKeyAsync(OperationName, key)).Should().BeNull();

        var recreated = await store.CreateAsync(OperationName, key);
        recreated.Created.Should().BeTrue();
        etags.Add(recreated.State.ETag);
        ids.Add(recreated.State.OperationId);

        etags.Should().OnlyHaveUniqueItems();
        ids.Should().OnlyHaveUniqueItems();
        (await store.GetByIdAsync(ids[0])).Should().BeNull();
    }

    [Test]
    public async Task Purge_Removes_Only_Expired_Finished_Operations()
    {
        var store = CreateStore(SuiteOptions());
        var active = await store.CreateAsync(OperationName, NewKey());
        var running = await CreateAndClaimAsync(store);
        var finished = await CreateAndClaimAsync(store);
        await store.CompleteAsync(finished, OperationCompletion.Succeeded());

        await CleanupAllAsync(store);
        (await store.GetByIdAsync(finished.OperationId)).Should().NotBeNull();

        await AdvanceAsync(SuiteOptions().TerminalRetention + TimeSpan.FromMilliseconds(500));
        await CleanupAllAsync(store);

        (await store.GetByIdAsync(finished.OperationId)).Should().BeNull();
        (await store.GetByIdAsync(active.State.OperationId)).Should().NotBeNull();
        (await store.GetByIdAsync(running.OperationId)).Should().NotBeNull();
    }

    [Test]
    public async Task AbandonAfter_Tracks_Activity_And_Clears_When_Finished()
    {
        var store = CreateStore(AbandonOptions());
        var timeout = AbandonOptions().InactivityTimeout;
        var tolerance = TimeSpan.FromMilliseconds(1);

        var created = (await store.CreateAsync(OperationName, NewKey())).State;
        created.AbandonAfter.Should().BeCloseTo(created.CreatedOn + timeout, tolerance);

        var claim = (await store.ClaimAsync(created.OperationId, "worker-a", ClaimDuration)).Claim!;
        (await store.GetByIdAsync(claim.OperationId))!.AbandonAfter.Should().BeCloseTo(claim.ExpiresOn + timeout, tolerance);

        claim = (await store.RenewAsync(claim, ClaimDuration))!;
        (await store.GetByIdAsync(claim.OperationId))!.AbandonAfter.Should().BeCloseTo(claim.ExpiresOn + timeout, tolerance);

        (await store.ReleaseAsync(claim, "E1")).Should().BeTrue();
        var released = (await store.GetByIdAsync(claim.OperationId))!;
        released.AbandonAfter.Should().BeCloseTo(released.UpdatedOn + timeout, tolerance);

        claim = (await store.ClaimAsync(created.OperationId, "worker-a", ClaimDuration)).Claim!;
        (await store.CompleteAsync(claim, OperationCompletion.Succeeded()))!.AbandonAfter.Should().BeNull();
        (await store.RequestCancelAsync((await store.CreateAsync(OperationName, NewKey())).State.OperationId))!.AbandonAfter.Should().BeNull();
    }

    [Test]
    public async Task Cleanup_Expires_Abandoned_Operations_Then_Purges()
    {
        var store = CreateStore(AbandonOptions());
        var accepted = (await store.CreateAsync(OperationName, NewKey())).State;
        var retrying = await CreateAndClaimAsync(store);
        (await store.ReleaseAsync(retrying, "Transient", "Broker timeout.")).Should().BeTrue();
        var stalled = await CreateAndClaimAsync(store);

        await AdvanceAsync(PastInactivity);
        await CleanupAllAsync(store);

        var expiredAccepted = (await store.GetByIdAsync(accepted.OperationId))!;
        expiredAccepted.Status.Should().Be(OperationStatus.Expired);
        expiredAccepted.IsFinished.Should().BeTrue();
        expiredAccepted.AbandonAfter.Should().BeNull();
        expiredAccepted.CompletedOn.Should().NotBeNull();
        expiredAccepted.PurgeAfter.Should().BeCloseTo(expiredAccepted.CompletedOn!.Value + AbandonOptions().TerminalRetention, TimeSpan.FromMilliseconds(1));
        expiredAccepted.ETag.Should().NotBe(accepted.ETag);

        var expiredRetrying = (await store.GetByIdAsync(retrying.OperationId))!;
        expiredRetrying.Status.Should().Be(OperationStatus.Expired);
        expiredRetrying.Attempt.Should().Be(1);
        expiredRetrying.ErrorCode.Should().Be("Transient");
        expiredRetrying.ErrorMessage.Should().Be("Broker timeout.");

        // The stalled claim is only abandoned once its claim expiry plus the inactivity timeout passes.
        (await store.GetByIdAsync(stalled.OperationId))!.Status.Should().Be(OperationStatus.Running);

        await AdvanceAsync(ClaimDuration);
        await CleanupAllAsync(store);

        var expiredStalled = (await store.GetByIdAsync(stalled.OperationId))!;
        expiredStalled.Status.Should().Be(OperationStatus.Expired);
        expiredStalled.ClaimOwner.Should().BeNull();
        expiredStalled.ClaimExpiresOn.Should().BeNull();

        var warnings = LogEntries.Where(e => e.Level == LogLevel.Warning).Select(e => e.Message).ToList();
        warnings.Should().ContainSingle(m => m.Contains(accepted.OperationId.ToString()) && m.Contains(nameof(OperationStatus.Accepted)));
        warnings.Should().ContainSingle(m => m.Contains(retrying.OperationId.ToString()) && m.Contains(nameof(OperationStatus.Retrying)) && m.Contains("Transient"));
        warnings.Should().ContainSingle(m => m.Contains(stalled.OperationId.ToString()) && m.Contains(nameof(OperationStatus.Running)));
        warnings.Should().NotContain(m => m.Contains(accepted.TenantId!) || m.Contains(accepted.InstanceId));

        await AdvanceAsync(AbandonOptions().TerminalRetention + TimeSpan.FromMilliseconds(500));
        await CleanupAllAsync(store);

        (await store.GetByIdAsync(accepted.OperationId)).Should().BeNull();
        (await store.GetByIdAsync(retrying.OperationId)).Should().BeNull();
        (await store.GetByIdAsync(stalled.OperationId)).Should().BeNull();
    }

    [Test]
    public async Task Cleanup_Does_Not_Expire_A_Renewed_Claim()
    {
        var store = CreateStore(AbandonOptions());
        var claim = await CreateAndClaimAsync(store);

        for (var i = 0; i < 3; i++)
        {
            await AdvanceAsync(TimeSpan.FromSeconds(1.5));
            claim = (await store.RenewAsync(claim, ClaimDuration))!;
            claim.Should().NotBeNull();
            await CleanupAllAsync(store);
        }

        (await store.GetByIdAsync(claim.OperationId))!.Status.Should().Be(OperationStatus.Running);
        (await store.CompleteAsync(claim, OperationCompletion.Succeeded()))!.Status.Should().Be(OperationStatus.Succeeded);
    }

    [Test]
    public async Task Expired_Is_Final_And_Fences_A_Late_Owner()
    {
        var store = CreateStore(AbandonOptions());
        var claim = await CreateAndClaimAsync(store);

        await AdvanceAsync(ClaimDuration + PastInactivity);
        await CleanupAllAsync(store);

        var expired = (await store.GetByIdAsync(claim.OperationId))!;
        expired.Status.Should().Be(OperationStatus.Expired);

        var reclaim = await store.ClaimAsync(claim.OperationId, "worker-b", ClaimDuration);
        reclaim.Outcome.Should().Be(OperationClaimOutcome.Finished);
        reclaim.Claim.Should().BeNull();

        (await store.CompleteAsync(claim, OperationCompletion.Succeeded())).Should().BeNull();
        (await store.ReleaseAsync(claim)).Should().BeFalse();
        (await store.RequestCancelAsync(claim.OperationId)).Should().Be(expired);
        (await store.GetByIdAsync(claim.OperationId)).Should().Be(expired);
    }
}
