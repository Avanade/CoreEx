using CoreEx.Database.Postgres.Test.Unit.Contracts;
using CoreEx.Database.Postgres.Test.Unit.Models;
using CoreEx.Database.Postgres.Test.Unit.Repository;
using CoreEx.EntityFrameworkCore;
using CoreEx.Mapping;
using CoreEx.Results;
using Microsoft.EntityFrameworkCore;

namespace CoreEx.Database.Postgres.Test.Unit;

public class EntityFrameworkBehaviorTests : DatabaseTestBase
{
    [Test]
    public void CheckModel_UsesInjectedExecutionContext_NotAmbient() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        // Ambient ExecutionContext.TenantId is "A" (see EntryPoint.cs). A caller may instead construct EfDb with an explicit,
        // non-ambient ExecutionContext (e.g. a background worker fanning out across tenants) - tenant checks must respect
        // that injected instance rather than falling back to the ambient one.
        var dc = ExecutionContext.GetRequiredService<TestDbContext>();
        var injectedContext = new ExecutionContext { TenantId = "Z" };
        var ef = new EfDb<TestDbContext>(dc, new EfDbOptions(), injectedContext);

        // Create stamps the model's TenantId from the injected context, not the ambient one.
        var m = new TestTable { Id = Runtime.NewGuid(), Text = "InjectedCtx", Flag = true };
        var created = await ef.Model<TestTable>().CreateWithResultAsync(m).ConfigureAwait(false);
        created.Value.Value.TenantId.Should().Be("Z");

        // The same injected-context EfDb must be able to read back what it just wrote.
        var got = await ef.Model<TestTable>().GetWithResultAsync(m.Id).ConfigureAwait(false);
        got.IsSuccess.Should().BeTrue();
        got.Value.TenantId.Should().Be("Z");
    }).AssertSuccess());

    [Test]
    public void Get_NullTenantId_ThrowsInvalidOperationException() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        // TableId 1 was seeded with no TenantId (see Data\data.yaml) - simulates a legacy/pre-tenancy row.
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();
        var act = async () => await ef.Table.GetWithResultAsync(1.ToGuid()).ConfigureAwait(false);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*TenantId is null or empty*");
    }).AssertSuccess());

    [Test]
    public void ToMappedItemsAsync_Collection_NullMapper_ThrowsArgumentNullException() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();
        var q = ef.Table.Query();

        IMapper<TestTable, TestTableDto> mapper = null!;
        var act = async () => await q.ToMappedItemsAsync<TestTable, List<TestTableDto>, TestTableDto>(mapper).ConfigureAwait(false);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("mapper");
    }).AssertSuccess());

    [Test]
    public void ToMappedItemsAsync_List_NullMapper_ThrowsArgumentNullException() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        // A null mapper must be rejected immediately, even when the query returns zero rows - otherwise the mapping delegate
        // that would dereference it never executes, and the null mapper goes unnoticed.
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();
        var q = ef.Table.Query().Where(x => x.Text == "DefinitelyDoesNotExist12345");

        IMapper<TestTable, TestTableDto> mapper = null!;
        var act = async () => await q.ToMappedItemsAsync(mapper).ConfigureAwait(false);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("mapper");
    }).AssertSuccess());

    [Test]
    public void ToMappedItemsResultAsync_NullMapper_ThrowsArgumentNullException() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();
        var q = ef.Table.Query();

        IMapper<TestTable, TestTableDto> mapper = null!;
        var act = async () => await q.ToMappedItemsResultAsync(mapper).ConfigureAwait(false);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("mapper");
    }).AssertSuccess());

    [Test]
    public void WithFilter_DefaultDoesNotAllowBypass() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var dc = ExecutionContext.GetRequiredService<TestDbContext>();
        var options = new EfDbOptions().WithModel<TestTable>(mo => mo.WithFilter(q => q.Where(x => x.Text == "Abc")));
        var ef = new EfDb<TestDbContext>(dc, options);

        var count = await ef.Model<TestTable>().Query().CountAsync().ConfigureAwait(false);
        count.Should().Be(1);

        // Even with BypassFilters=true, a filter registered without an explicit allowFilterBypass must not be bypassable.
        var countBypassed = await ef.Model<TestTable>().Query(new EfDbArgs { BypassFilters = true }).CountAsync().ConfigureAwait(false);
        countBypassed.Should().Be(1);
    }).AssertSuccess());

    [Test]
    public void Upsert_CreatesWhenNotFound() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();
        var id = Runtime.NewGuid();
        var m = new TestTable { Id = id, Text = "Upserted", Flag = true };

        var r = await ef.Table.UpsertAsync(m).ConfigureAwait(false);
        r.WasMutated.Should().BeTrue();
        r.Value.Text.Should().Be("Upserted");

        var got = await ef.Table.GetAsync(id).ConfigureAwait(false);
        got.Should().NotBeNull();
        got.Text.Should().Be("Upserted");
    }).AssertSuccess());

    [Test]
    public void Upsert_UpdatesWhenFound() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var id = 6.ToGuid();
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();

        var m = await ef.Table.GetAsync(id).ConfigureAwait(false);
        m.Should().NotBeNull();
        m.Text += "-Upsert";

        var r = await ef.Table.UpsertAsync(m).ConfigureAwait(false);
        r.WasMutated.Should().BeTrue();
        r.Value.Text.Should().Be(m.Text);
    }).AssertSuccess());

    [Test]
    public void QueryTracked_EntitiesAreTracked() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();
        var dc = ExecutionContext.GetRequiredService<TestDbContext>();
        dc.ChangeTracker.Clear();

        var tracked = await ef.Table.QueryTracked().FirstAsync(x => x.Id == 2.ToGuid()).ConfigureAwait(false);
        dc.ChangeTracker.Entries().Should().Contain(e => e.Entity == tracked);

        dc.ChangeTracker.Clear();
        var untracked = await ef.Table.Query().FirstAsync(x => x.Id == 2.ToGuid()).ConfigureAwait(false);
        dc.ChangeTracker.Entries().Should().NotContain(e => e.Entity == untracked);
    }).AssertSuccess());

    [Test]
    public void ClearChangeTrackerAfterGet_DetachesUnrelatedTrackedEntities() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();
        var dc = ExecutionContext.GetRequiredService<TestDbContext>();
        dc.ChangeTracker.Clear();

        // Simulate another repository call within the same scoped DbContext/unit of work having a tracked entity in flight.
        var other = await ef.Table.QueryTracked().FirstAsync(x => x.Id == 3.ToGuid()).ConfigureAwait(false);
        dc.ChangeTracker.Entries().Should().Contain(e => e.Entity == other);

        var args = ef.Table.Args with { ClearChangeTrackerAfterGet = true };
        var m = await ef.Table.GetAsync(args, 2.ToGuid()).ConfigureAwait(false);
        m.Should().NotBeNull();

        // ChangeTracker.Clear() detaches the entire context, not just the row just fetched - the unrelated entity is gone too.
        dc.ChangeTracker.Entries().Should().NotContain(e => e.Entity == other);
    }).AssertSuccess());

    [Test]
    public void WithOnBeforeCreateOrUpdate_FailureShortCircuitsCreate() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var dc = ExecutionContext.GetRequiredService<TestDbContext>();
        var options = new EfDbOptions().WithModel<TestTable>(mo => mo.WithOnBeforeCreateOrUpdate((m, _) => m.Text == "Blocked" ? Result.ValidationError("Text 'Blocked' is not allowed.") : Result.Success));
        var ef = new EfDb<TestDbContext>(dc, options);

        var r = await ef.Model<TestTable>().CreateWithResultAsync(new TestTable { Id = Runtime.NewGuid(), Text = "Blocked", Flag = true, TenantId = "A" }).ConfigureAwait(false);
        r.IsValidationError.Should().BeTrue();

        var allowed = await ef.Model<TestTable>().CreateWithResultAsync(new TestTable { Id = Runtime.NewGuid(), Text = "Allowed", Flag = true, TenantId = "A" }).ConfigureAwait(false);
        allowed.IsSuccess.Should().BeTrue();
    }).AssertSuccess());

    [Test]
    public void WithUpdateModelMapper_CustomMapperInvokedForDetachedUpdate() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();
        var dc = ExecutionContext.GetRequiredService<TestDbContext>();

        var id = 9.ToGuid();
        var m = await ef.Table.GetAsync(id).ConfigureAwait(false);
        m.Should().NotBeNull();
        dc.ChangeTracker.Clear();

        var originalNumber = m.Number;
        m.Text += "-Custom";
        m.Number = (m.Number ?? 0) + 1000;

        // A custom options instance sharing the same underlying DbContext, but with an updateModelMapper that only copies Text (ignores Number).
        var options = new EfDbOptions().WithModel<TestTable>(mo => mo.WithUpdateModelMapper((update, existing) =>
        {
            existing.Text = update.Text;
            return true;
        }));
        var customEf = new EfDb<TestDbContext>(dc, options);

        var u = await customEf.Model<TestTable>().UpdateAsync(m).ConfigureAwait(false);
        u.Value.Text.Should().Be(m.Text);
        u.Value.Number.Should().Be(originalNumber); // Number change was ignored by the custom mapper.
    }).AssertSuccess());
}
