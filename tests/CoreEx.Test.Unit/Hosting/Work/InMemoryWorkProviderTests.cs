using CoreEx.Hosting.Work;

namespace CoreEx.Test.Unit.Hosting.Work;

[TestFixture]
public class InMemoryWorkProviderTests
{
    private static WorkState State(string id, string domainName = "CoreEx.Test.Unit", string? tenantId = null, string operationName = "T") => new()
    {
        DomainName = domainName,
        TenantId = tenantId,
        OperationName = operationName,
        Id = id,
        TypeName = "T"
    };

    private static Task<WorkSnapshot> GetAsync(InMemoryWorkProvider provider, string id) => GetAsync(provider, State(id));

    private static Task<WorkSnapshot> GetAsync(InMemoryWorkProvider provider, WorkState state)
        => provider.GetAsync(state.DomainName, state.TenantId, state.OperationName, state.Id ?? throw new InvalidOperationException("Missing work identifier."), default);

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
        var absent = await GetAsync(provider, "absent");
        absent.State.Should().BeNull();
        absent.Now.Should().Be(time.Now);

        var state = State("abc");
        state.Status = WorkStatus.Created;
        (await provider.CreateAsync(state, TimeSpan.FromMinutes(1), default)).Should().BeTrue();
        var present = await GetAsync(provider, "abc");
        present.State!.ETag.Should().Be(state.ETag);
        present.Now.Should().Be(time.Now);
    }

    [Test]
    public async Task Update_And_Delete_Use_ETag()
    {
        var provider = new InMemoryWorkProvider();
        var state = State("abc");
        state.Status = WorkStatus.Created;
        (await provider.CreateAsync(state, TimeSpan.FromMinutes(1), default)).Should().BeTrue();
        (await provider.CreateAsync(State("abc"), TimeSpan.FromMinutes(1), default)).Should().BeFalse();

        var first = (await GetAsync(provider, "abc")).State!;
        var stale = (await GetAsync(provider, "abc")).State!;
        first.Status = WorkStatus.Started;
        (await provider.UpdateAsync(first, TimeSpan.FromMinutes(1), default)).Should().BeTrue();
        first.ETag.Should().NotBe(stale.ETag);

        stale.Status = WorkStatus.Canceled;
        (await provider.UpdateAsync(stale, TimeSpan.FromMinutes(1), default)).Should().BeFalse();
        (await provider.DeleteAsync(stale, default)).Should().BeFalse();

        first.Status = WorkStatus.Completed;
        (await provider.UpdateAsync(first, TimeSpan.FromMinutes(1), default)).Should().BeTrue();
        (await GetAsync(provider, "abc")).State!.Status.Should().Be(WorkStatus.Completed);

        (await provider.DeleteAsync(first, default)).Should().BeTrue();
        (await GetAsync(provider, "abc")).State.Should().BeNull();
        (await provider.DeleteAsync(first, default)).Should().BeTrue();
    }

    [Test]
    public async Task Records_Expire_Using_The_Provider_Clock()
    {
        var time = new TestTimeProvider();
        var provider = new InMemoryWorkProvider(timeProvider: time);
        (await provider.CreateAsync(State("abc"), TimeSpan.FromMinutes(1), default)).Should().BeTrue();
        time.Now = time.Now.AddMinutes(2);
        (await GetAsync(provider, "abc")).State.Should().BeNull();
        (await provider.CreateAsync(State("abc"), TimeSpan.FromMinutes(1), default)).Should().BeTrue();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task RecreatedRecord_Rejects_Previous_Incarnation(bool expire)
    {
        var time = new TestTimeProvider();
        var provider = new InMemoryWorkProvider(timeProvider: time);
        var original = State("abc");
        var ttl = TimeSpan.FromMinutes(1);
        (await provider.CreateAsync(original, ttl, default)).Should().BeTrue();
        (await provider.UpdateAsync(original, ttl, default)).Should().BeTrue();
        var stale = (await GetAsync(provider, "abc")).State!;

        if (expire)
            time.Now = time.Now.AddMinutes(2);
        else
            (await provider.DeleteAsync(original, default)).Should().BeTrue();

        var replacement = State("abc");
        (await provider.CreateAsync(replacement, ttl, default)).Should().BeTrue();
        (await provider.UpdateAsync(replacement, ttl, default)).Should().BeTrue();
        replacement.ETag.Should().NotBe(stale.ETag);
        (await provider.UpdateAsync(stale, ttl, default)).Should().BeFalse();
        (await provider.DeleteAsync(stale, default)).Should().BeFalse();
        (await GetAsync(provider, "abc")).State!.ETag.Should().Be(replacement.ETag);
    }

    [Test]
    public async Task Same_InstanceId_Is_Isolated_By_Domain_And_Tenant()
    {
        var provider = new InMemoryWorkProvider();
        var tenantA = State("same-id", "orders", "tenant-a", "import");
        var tenantB = State("same-id", "orders", "tenant-b", "import");
        var domainWide = State("same-id", "orders", null, "import");
        var otherDomain = State("same-id", "products", "tenant-a", "import");

        foreach (var state in new[] { tenantA, tenantB, domainWide, otherDomain })
            (await provider.CreateAsync(state, TimeSpan.FromMinutes(1), default)).Should().BeTrue();

        (await GetAsync(provider, tenantA)).State!.TenantId.Should().Be("tenant-a");
        (await GetAsync(provider, tenantB)).State!.TenantId.Should().Be("tenant-b");
        (await GetAsync(provider, domainWide)).State!.TenantId.Should().BeNull();
        (await GetAsync(provider, otherDomain)).State!.DomainName.Should().Be("products");
    }

    [Test]
    public async Task Empty_Tenant_Is_DomainWide()
    {
        var provider = new InMemoryWorkProvider();
        var id = "same-id";
        var state = State(id, tenantId: "");

        state.TenantId.Should().BeNull();
        (await provider.CreateAsync(state, TimeSpan.FromMinutes(1), default)).Should().BeTrue();
        var read = await provider.GetAsync(state.DomainName, "", state.OperationName, id, default);
        read.State!.TenantId.Should().BeNull();
        (await GetAsync(provider, state)).State!.ETag.Should().Be(read.State.ETag);
    }

    [TestCase(" ", "import", "task")]
    [TestCase("orders", " ", "task")]
    [TestCase("orders", "import", " ")]
    public async Task Writes_Reject_Blank_Identity_Components(string domainName, string operationName, string id)
    {
        var provider = new InMemoryWorkProvider();
        var state = State(id, domainName, operationName: operationName);
        var ttl = TimeSpan.FromMinutes(1);
        Func<Task>[] writes =
        [
            () => provider.CreateAsync(state, ttl, default),
            () => provider.UpdateAsync(state, ttl, default),
            () => provider.DeleteAsync(state, default)
        ];

        foreach (var write in writes)
            await write.Should().ThrowAsync<ArgumentException>();
    }
}
