using CoreEx.Database.SqlServer.Test.Unit.Models;
using CoreEx.Database.SqlServer.Test.Unit.Repository;
using CoreEx.Results;

namespace CoreEx.Database.SqlServer.Test.Unit;

public partial class EntityFrameworkCrudTests 
{
    [Test]
    public void Update_IsDeleted() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();
        var m = new TestTable
        {
            Id = 1.ToGuid(),
            IsDeleted = true
        };

        var act = async () => await ef.Table.UpdateWithResultAsync(m).ConfigureAwait(false);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Cannot update a model and set to the deleted state (IsDeleted must be false); use the delete operation to perform.*");
    }).AssertSuccess());

    [Test]
    public void Update_NotFound() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();
        var m = new TestTable
        {
            Id = 404.ToGuid()
        };

        var r = await ef.Table.UpdateWithResultAsync(m).ConfigureAwait(false);
        r.IsNotFoundError.Should().BeTrue();
    }).AssertSuccess());

    [Test]
    public void Update_Concurrency() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();
        var m = new TestTable
        {
            Id = 8.ToGuid(),
            ETag = "InvalidETag"
        };

        var r = await ef.Table.UpdateWithResultAsync(m).ConfigureAwait(false);
        r.IsConcurrencyError.Should().BeTrue();
    }).AssertSuccess());

    [Test]
    public void Update_Duplicate_Text() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();

        // Pre-get existing before modification.
        var m = await ef.Table.GetAsync(8.ToGuid()).ConfigureAwait(false);
        m.Should().NotBeNull();

        // Modify a pre-existing value.
        m.Text = "Abc";

        // Update it.
        var act = async () => await ef.Table.UpdateAsync(m).ConfigureAwait(false);
        await act.Should().ThrowAsync<DuplicateException>();
    }).AssertSuccess());

    [Test]
    public void Update_Success_Attached() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var id = 8.ToGuid();
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();
        var dc = ExecutionContext.GetRequiredService<TestDbContext>();

        // Pre-get existing before modification.
        var m = await ef.Table.GetAsync(id).ConfigureAwait(false);
        m.Should().NotBeNull();

        // Modify a property.
        m.Text += "XXX";

        // Update it.
        var u = await ef.Table.UpdateAsync(m).ConfigureAwait(false);
        u.WasMutated.Should().BeTrue();
        u.Value.Text.Should().Be(m.Text);
        u.Value.UpdatedBy.Should().NotBeNull();
        u.Value.UpdatedOn.Should().NotBeNull();

        // Re-get to ensure updated.
        dc.ChangeTracker.Clear();
        var r = await ef.Table.GetAsync(id).ConfigureAwait(false);
        r.Should().NotBeNull();
        r.Text.Should().Be(m.Text);

        ObjectComparer.Assert(u.Value, r);
    }).AssertSuccess());

    [Test]
    public void Update_Success_Detached() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var id = 8.ToGuid();
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();
        var dc = ExecutionContext.GetRequiredService<TestDbContext>();

        // Pre-get existing before modification.
        var m = await ef.Table.GetAsync(id).ConfigureAwait(false);
        m.Should().NotBeNull();
        dc.ChangeTracker.Clear();

        //Modify a property.
        m.Text += "YYY";

        // Update it.
        var u = await ef.Table.UpdateAsync(m).ConfigureAwait(false);
        u.WasMutated.Should().BeTrue();
        u.Value.Text.Should().Be(m.Text);
        u.Value.UpdatedBy.Should().NotBeNull();
        u.Value.UpdatedOn.Should().NotBeNull();

        // Re-get to ensure updated.
        dc.ChangeTracker.Clear();
        var r = await ef.Table.GetAsync(id).ConfigureAwait(false);
        r.Should().NotBeNull();
        r.Text.Should().Be(m.Text);

        ObjectComparer.Assert(u.Value, r);
    }).AssertSuccess());

    [Test]
    public void Update_Mapped_Success() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var id = 8.ToGuid();
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();
        var dc = ExecutionContext.GetRequiredService<TestDbContext>();

        // Pre-get existing before modification.
        var v = await ef.TableDto.GetAsync(id).ConfigureAwait(false);
        v.Should().NotBeNull();

        // Modify a value.
        v.Text += "ZZZ";

        // Update it.
        var u = await ef.TableDto.UpdateAsync(v).ConfigureAwait(false);
        u.Value.Text.Should().Be(v.Text);
        u.Value.ChangeLog.Should().NotBeNull();
        u.Value.ChangeLog.UpdatedBy.Should().NotBeNull();
        u.Value.ChangeLog.UpdatedOn.Should().NotBeNull();

        // Re-get to ensure updated.
        dc.ChangeTracker.Clear();
        var r = await ef.TableDto.GetAsync(id).ConfigureAwait(false);
        r.Should().NotBeNull();
        r.Text.Should().Be(v.Text);

        ObjectComparer.Assert(u.Value, r);
    }).AssertSuccess());
}