namespace CoreEx.Test.Unit.Operations;

[TestFixture]
public class InMemoryOperationStoreTests : OperationStoreTestsBase
{
    private TestTimeProvider _time = null!;

    /// <summary>Provides a controllable clock for store tests.</summary>
    private sealed class TestTimeProvider : TimeProvider
    {
        /// <summary>Gets or sets the current test time.</summary>
        public DateTimeOffset Now { get; set; } = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);

        /// <inheritdoc/>
        public override DateTimeOffset GetUtcNow() => Now;
    }

    [SetUp]
    public void SetUp() => _time = new TestTimeProvider();

    protected override OperationStoreBase CreateStore(OperationStoreOptions options) => new InMemoryOperationStore(CreateLogger<InMemoryOperationStore>(), Settings, options, _time);

    protected override Task AdvanceAsync(TimeSpan duration)
    {
        _time.Now += duration;
        return Task.CompletedTask;
    }

    [Test]
    public async Task Store_Time_Is_Used_For_Timestamps()
    {
        var store = CreateStore(SuiteOptions());

        var created = await store.CreateAsync(OperationName, NewKey());

        created.State.CreatedOn.Should().Be(_time.Now);
    }

    [Test]
    public async Task Arguments_Are_Validated()
    {
        var store = CreateStore(SuiteOptions());
        var claim = new OperationClaim(Guid.NewGuid(), "worker-a", 1, _time.Now, false);
        var key = NewKey();

        await FluentActions.Awaiting(() => store.GetByIdAsync(Guid.Empty)).Should().ThrowAsync<ArgumentException>();
        await FluentActions.Awaiting(() => store.GetByKeyAsync(null!, key)).Should().ThrowAsync<ArgumentNullException>();
        await FluentActions.Awaiting(() => store.CreateAsync(OperationName, key, new string('x', OperationLimits.MaxInitiatorIdLength + 1))).Should().ThrowAsync<ArgumentException>().WithParameterName("initiatorId");
        await FluentActions.Awaiting(() => store.ClaimAsync(Guid.NewGuid(), " ", ClaimDuration)).Should().ThrowAsync<ArgumentException>();
        await FluentActions.Awaiting(() => store.ClaimAsync(Guid.NewGuid(), new string('x', OperationLimits.MaxClaimOwnerLength + 1), ClaimDuration)).Should().ThrowAsync<ArgumentException>().WithParameterName("owner");
        await FluentActions.Awaiting(() => store.ClaimAsync(Guid.NewGuid(), "worker-a", TimeSpan.Zero)).Should().ThrowAsync<ArgumentOutOfRangeException>().WithParameterName("duration");
        await FluentActions.Awaiting(() => store.RenewAsync(claim, ClaimDuration, new string('x', OperationLimits.MaxProgressLength + 1))).Should().ThrowAsync<ArgumentException>().WithParameterName("progress");
        await FluentActions.Awaiting(() => store.ReleaseAsync(claim, null, "message")).Should().ThrowAsync<ArgumentException>().WithParameterName("errorMessage");
        await FluentActions.Awaiting(() => store.ReleaseAsync(claim, " ")).Should().ThrowAsync<ArgumentException>().WithParameterName("errorCode");
        await FluentActions.Awaiting(() => store.ReleaseAsync(claim, new string('x', OperationLimits.MaxErrorCodeLength + 1))).Should().ThrowAsync<ArgumentException>().WithParameterName("errorCode");
        await FluentActions.Awaiting(() => store.ReleaseAsync(claim, "E1", new string('x', OperationLimits.MaxErrorMessageLength + 1))).Should().ThrowAsync<ArgumentException>().WithParameterName("errorMessage");
        await FluentActions.Awaiting(() => store.CleanupAsync(0)).Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Test]
    public void Completion_And_Options_Are_Validated()
    {
        FluentActions.Invoking(() => new InMemoryOperationStore(null!, Settings)).Should().Throw<ArgumentNullException>().WithParameterName("logger");
        FluentActions.Invoking(() => new InMemoryOperationStore(CreateLogger<InMemoryOperationStore>(), null!)).Should().Throw<ArgumentNullException>().WithParameterName("settings");
        FluentActions.Invoking(() => OperationCompletion.Failed(" ")).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => OperationCompletion.Failed(new string('x', OperationLimits.MaxErrorCodeLength + 1))).Should().Throw<ArgumentException>().WithParameterName("errorCode");
        FluentActions.Invoking(() => OperationCompletion.Failed("E", new string('x', OperationLimits.MaxErrorMessageLength + 1))).Should().Throw<ArgumentException>().WithParameterName("errorMessage");
        FluentActions.Invoking(() => OperationCompletion.Succeeded(new string('x', OperationLimits.MaxResultLocationLength + 1))).Should().Throw<ArgumentException>().WithParameterName("resultLocation");
        FluentActions.Invoking(() => new OperationStoreOptions { TerminalRetention = TimeSpan.Zero }).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => new OperationStoreOptions { InactivityTimeout = TimeSpan.Zero }).Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void Options_UseConfiguredDefaults_AndAllowExplicitOverrides()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CoreEx:Operations:InactivityTimeout"] = "02:00:00",
                ["CoreEx:Operations:TerminalRetention"] = "03:00:00"
            })
            .Build();

        var options = new OperationStoreOptions(configuration) { TerminalRetention = TimeSpan.FromHours(4) };

        options.InactivityTimeout.Should().Be(TimeSpan.FromHours(2));
        options.TerminalRetention.Should().Be(TimeSpan.FromHours(4));
        new OperationStoreOptions(CoreEx.Abstractions.Internal.EmptyConfiguration).InactivityTimeout.Should().Be(TimeSpan.FromHours(24));
        new OperationStoreOptions(CoreEx.Abstractions.Internal.EmptyConfiguration).TerminalRetention.Should().Be(TimeSpan.FromHours(24));
    }

    [Test]
    public void Options_RejectInvalidConfiguredDefaults()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["CoreEx:Operations:InactivityTimeout"] = "00:00:00" })
            .Build();

        FluentActions.Invoking(() => new OperationStoreOptions(configuration))
            .Should().Throw<ArgumentOutOfRangeException>().WithParameterName("InactivityTimeout");
    }
}
