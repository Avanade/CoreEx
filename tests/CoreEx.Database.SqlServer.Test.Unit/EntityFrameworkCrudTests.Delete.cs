using CoreEx.Database.SqlServer.Test.Unit.Models;
using CoreEx.Database.SqlServer.Test.Unit.Repository;
using CoreEx.Results;
using CoreEx.EntityFrameworkCore;

namespace CoreEx.Database.SqlServer.Test.Unit;

public partial class EntityFrameworkCrudTests 
{
    [Test]
    public void Delete_IsDeleted() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();
        var r = await ef.Table.DeleteAsync(10.ToGuid()).ConfigureAwait(false);
        r.WasMutated.Should().BeFalse();
    }).AssertSuccess());

    [Test]
    public void Delete_NotFound() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();
        var r = await ef.Table.DeleteAsync(404.ToGuid()).ConfigureAwait(false);
        r.WasMutated.Should().BeFalse();
    }).AssertSuccess());

    [Test]
    public void Delete_NotAuthorized() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();
        var r = await ef.Table.DeleteWithResultAsync(7.ToGuid()).ConfigureAwait(false);
        r.IsAuthorizationError.Should().BeTrue();
    }).AssertSuccess());

    [Test]
    public void Delete_Mapped_Success() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        // NOTE: The TableDto is a proxy over the Table, so no need to test independently.

        var id = 9.ToGuid();
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();
        var dc = ExecutionContext.GetRequiredService<TestDbContext>();

        // Pre-get existing before modification.
        var m = await ef.TableDto.GetAsync(id).ConfigureAwait(false);
        m.Should().NotBeNull();

        // Delete it.
        var r = await ef.TableDto.DeleteAsync(id).ConfigureAwait(false);
        r.WasMutated.Should().BeTrue();

        // Delete it again.
        dc.ChangeTracker.Clear();
        r = await ef.TableDto.DeleteAsync(id).ConfigureAwait(false);
        r.WasMutated.Should().BeFalse();

        // Re-get to ensure Deleted.
        dc.ChangeTracker.Clear();
        m = await ef.TableDto.GetAsync(id).ConfigureAwait(false);
        m.Should().BeNull();
    }).AssertSuccess());
}