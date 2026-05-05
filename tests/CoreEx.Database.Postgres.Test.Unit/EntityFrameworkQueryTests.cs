using CoreEx.Data;
using CoreEx.Data.Querying;
using CoreEx.Database.Postgres.Test.Unit.Contracts;
using CoreEx.Database.Postgres.Test.Unit.Models;
using CoreEx.Database.Postgres.Test.Unit.Repository;
using CoreEx.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CoreEx.Database.Postgres.Test.Unit;

public class EntityFrameworkQueryTests : DatabaseTestBase
{
    [Test]
    public void Query_ByPassFilters() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();
        var q = ef.Table.Query();
        var c = await q.CountAsync().ConfigureAwait(false);
        c.Should().Be(5);   // Excludes deleted, not-authorized and wrong-tenant.

        q = ef.Table.Query(new EntityFrameworkCore.EfDbArgs { BypassFilters = true });
        c = await q.CountAsync().ConfigureAwait(false);
        c.Should().Be(7);  // Excludes wrong-tenant (configured as not bypassable).
    }).AssertSuccess());

    [Test]
    public void Query_ToItemsResult() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();
        var q = ef.Table.Query().OrderBy(x => x.Text);

        var ir = await q.ToItemsResultAsync().ConfigureAwait(false);
        ir.Should().NotBeNull();
        ir.Items.Should().NotBeNull();
        ir.Items.Count().Should().Be(5);
        ir.Paging.Should().NotBeNull();
        ir.Paging.Skip.Should().Be(0);
        ir.Paging.Take.Should().Be(PagingArgs.DefaultTake);
        ir.Paging.TotalCount.Should().BeNull();
        ir.Items.Select(x => x.Text).Should().BeEquivalentTo(["Abc", "Abz", "Ace", "Jkl", "Zyi"], options => options.WithStrictOrdering());

    }).AssertSuccess());

    [Test]
    public void Query_ToItemsResult_WithPaging() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();
        var q = ef.Table.Query().OrderBy(x => x.Text);

        var ir = await q.ToItemsResultAsync(new PagingArgs(2, 2)).ConfigureAwait(false);
        ir.Should().NotBeNull();
        ir.Items.Should().NotBeNull();
        ir.Items.Count().Should().Be(2);
        ir.Paging.Should().NotBeNull();
        ir.Paging.Skip.Should().Be(2);
        ir.Paging.Take.Should().Be(2);
        ir.Paging.TotalCount.Should().BeNull();
        ir.Items.Select(x => x.Text).Should().BeEquivalentTo(["Ace", "Jkl"], options => options.WithStrictOrdering());

    }).AssertSuccess());

    [Test]
    public void Query_ToItemsResult_WithPaging_Count() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();
        var q = ef.Table.Query().OrderBy(x => x.Text);

        var ir = await q.ToItemsResultAsync(new PagingArgs(2, 2, true)).ConfigureAwait(false);
        ir.Should().NotBeNull();
        ir.Items.Should().NotBeNull();
        ir.Items.Count().Should().Be(2);
        ir.Paging.Should().NotBeNull();
        ir.Paging.Skip.Should().Be(2);
        ir.Paging.Take.Should().Be(2);
        ir.Paging.TotalCount.Should().Be(5);
        ir.Items.Select(x => x.Text).Should().BeEquivalentTo(["Ace", "Jkl"], options => options.WithStrictOrdering());
    }).AssertSuccess());

    [Test]
    public void Query_ToMappedItemsResult() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();
        var q = ef.Table.Query().OrderBy(x => x.Text);

        var ir = await q.ToMappedItemsResultAsync(TestTableDtoMapper.From).ConfigureAwait(false);
        ir.Should().NotBeNull();
        ir.Items.Should().NotBeNull();
        ir.Items.Count().Should().Be(5);
        ir.Paging.Should().NotBeNull();
        ir.Paging.Skip.Should().Be(0);
        ir.Paging.Take.Should().Be(PagingArgs.DefaultTake);
        ir.Paging.TotalCount.Should().BeNull();
        ir.Items.Select(x => x.Text).Should().BeEquivalentTo(["Abc", "Abz", "Ace", "Jkl", "Zyi"], options => options.WithStrictOrdering());

        // Mapping worked?
        var item = ir.Items.First();
        item.Id.Should().Be(2.ToGuid());
        item.Text.Should().Be("Abc");
        item.Number.Should().Be(123);
        item.Amount.Should().Be(45.67m);
        item.Date.Should().Be(new DateOnly(2024, 06, 20));
        item.Time.Should().Be(new TimeOnly(14, 30, 59));
        item.Key.Should().NotBeNull();
        item.Key.Key.Should().Be("Value");
        item.ETag.Should().NotBeNullOrEmpty();
        item.ChangeLog.Should().NotBeNull();
        item.ChangeLog.CreatedBy.Should().NotBeNull();
        item.ChangeLog.CreatedOn.Should().NotBeNull();
        item.ChangeLog.UpdatedBy.Should().BeNull();
        item.ChangeLog.UpdatedOn.Should().BeNull();
    }).AssertSuccess());

    [Test]
    public void Query_ToMappedtemsResult_WithPaging() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();
        var q = ef.Table.Query().OrderBy(x => x.Text);

        var ir = await q.ToMappedItemsResultAsync(TestTableDtoMapper.From, new PagingArgs(2, 2)).ConfigureAwait(false);
        ir.Should().NotBeNull();
        ir.Items.Should().NotBeNull();
        ir.Items.Count().Should().Be(2);
        ir.Paging.Should().NotBeNull();
        ir.Paging.Skip.Should().Be(2);
        ir.Paging.Take.Should().Be(2);
        ir.Paging.TotalCount.Should().BeNull();
        ir.Items.Select(x => x.Text).Should().BeEquivalentTo(["Ace", "Jkl"], options => options.WithStrictOrdering());
    }).AssertSuccess());

    [Test]
    public void Query_ToMappedItemsResult_WithPaging_Count() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();
        var q = ef.Table.Query().OrderBy(x => x.Text);

        var ir = await q.ToMappedItemsResultAsync(TestTableDtoMapper.From, new PagingArgs(1, 3, true)).ConfigureAwait(false);
        ir.Should().NotBeNull();
        ir.Items.Should().NotBeNull();
        ir.Items.Count().Should().Be(3);
        ir.Paging.Should().NotBeNull();
        ir.Paging.Skip.Should().Be(1);
        ir.Paging.Take.Should().Be(3);
        ir.Paging.TotalCount.Should().Be(5);
        ir.Items.Select(x => x.Text).Should().BeEquivalentTo(["Abz", "Ace", "Jkl"], options => options.WithStrictOrdering());
    }).AssertSuccess());

    [Test]
    public void Query_Dynamic_Filter() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();
        var qa = new QueryArgs { Filter = "startswith(text, 'a')" };
        var q = ef.Table.Query().Where(_queryArgsConfig, qa).OrderBy(_queryArgsConfig, qa);
        var ir = await q.ToItemsResultAsync(PagingArgs.None).ConfigureAwait(false);

        ir.Should().NotBeNull();
        ir.Items.Should().NotBeNull();
        ir.Items.Count().Should().Be(3);
        ir.Items.Select(x => x.Text).Should().BeEquivalentTo(["Abc", "Abz", "Ace"], options => options.WithStrictOrdering());
        ir.Paging.Should().BeNull();
    }).AssertSuccess());

    private static readonly QueryArgsConfig _queryArgsConfig = QueryArgsConfig.Create()
        .WithFilter(f => f.AddField<string>(nameof(TestTable.Text), c => c.AsLowerCase().WithOperators(QueryFilterOperator.AllStringOperators)))
        .WithOrderBy(o => o.AddField(nameof(TestTable.Text), c => c.WithDefault()));
}