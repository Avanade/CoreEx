using CoreEx.Hosting.Work;

namespace CoreEx.Test.Unit.Hosting.Work;

[TestFixture]
public class InMemoryWorkProviderTests
{
    private static OperationKey Key(string id) => OperationKey.ForDomain("CoreEx.Test.Unit", "T", id);

    private static WorkState State(string id)
    {
        var key = Key(id);
        return new() { Scope = key.Scope, DomainName = key.DomainName, TenantId = key.TenantId, OperationName = key.OperationName, Id = id, TypeName = "T" };
    }

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
        var absent = await provider.GetAsync(Key("absent"), default);
        absent.State.Should().BeNull();
        absent.Now.Should().Be(time.Now);

        var state = State("abc");
        state.Status = WorkStatus.Created;
        (await provider.CreateAsync(state, TimeSpan.FromMinutes(1), default)).Should().BeTrue();
        var present = await provider.GetAsync(Key("abc"), default);
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

        var first = (await provider.GetAsync(Key("abc"), default)).State!;
        var stale = (await provider.GetAsync(Key("abc"), default)).State!;
        first.Status = WorkStatus.Started;
        (await provider.UpdateAsync(first, TimeSpan.FromMinutes(1), default)).Should().BeTrue();
        first.ETag.Should().NotBe(stale.ETag);

        stale.Status = WorkStatus.Canceled;
        (await provider.UpdateAsync(stale, TimeSpan.FromMinutes(1), default)).Should().BeFalse();
        (await provider.DeleteAsync(stale, default)).Should().BeFalse();

        first.Status = WorkStatus.Completed;
        (await provider.UpdateAsync(first, TimeSpan.FromMinutes(1), default)).Should().BeTrue();
        (await provider.GetAsync(Key("abc"), default)).State!.Status.Should().Be(WorkStatus.Completed);

        (await provider.DeleteAsync(first, default)).Should().BeTrue();
        (await provider.GetAsync(Key("abc"), default)).State.Should().BeNull();
        (await provider.DeleteAsync(first, default)).Should().BeTrue();
    }

    [Test]
    public async Task Records_Expire_Using_The_Provider_Clock()
    {
        var time = new TestTimeProvider();
        var provider = new InMemoryWorkProvider(timeProvider: time);
        (await provider.CreateAsync(State("abc"), TimeSpan.FromMinutes(1), default)).Should().BeTrue();
        time.Now = time.Now.AddMinutes(2);
        (await provider.GetAsync(Key("abc"), default)).State.Should().BeNull();
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
        var stale = (await provider.GetAsync(Key("abc"), default)).State!;

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
        (await provider.GetAsync(Key("abc"), default)).State!.ETag.Should().Be(replacement.ETag);
    }

    [Test]
    public async Task Same_InstanceId_Is_Isolated_By_Tenant_And_Explicit_Domain_Scope()
    {
        var provider = new InMemoryWorkProvider();
        var tenantA = OperationKey.ForTenant("orders", "tenant-a", "import", "same-id");
        var tenantB = OperationKey.ForTenant("orders", "tenant-b", "import", "same-id");
        var domainWide = OperationKey.ForDomain("orders", "import", "same-id");

        foreach (var key in new[] { tenantA, tenantB, domainWide })
            (await provider.CreateAsync(State(key), TimeSpan.FromMinutes(1), default)).Should().BeTrue();

        (await provider.GetAsync(tenantA, default)).State!.OperationKey.TenantId.Should().Be("tenant-a");
        (await provider.GetAsync(tenantB, default)).State!.OperationKey.TenantId.Should().Be("tenant-b");
        (await provider.GetAsync(domainWide, default)).State!.OperationKey.Scope.Should().Be(OperationKeyScope.DomainWide);
    }

    private static WorkState State(OperationKey key) => new()
    {
        Scope = key.Scope,
        DomainName = key.DomainName,
        TenantId = key.TenantId,
        OperationName = key.OperationName,
        Id = key.InstanceId,
        TypeName = "T"
    };
}
