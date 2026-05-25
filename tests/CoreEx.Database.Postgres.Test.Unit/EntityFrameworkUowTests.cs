using CoreEx.Data;
using CoreEx.Database.Postgres.Test.Unit.Repository;
using CoreEx.Results;

namespace CoreEx.Database.Postgres.Test.Unit;

public class EntityFrameworkUowTests : DatabaseTestBase
{
    [Test]
    public void Rollback_On_Exception() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();
        var uow = ExecutionContext.GetRequiredService<IUnitOfWork>();
        var dc = ExecutionContext.GetRequiredService<TestDbContext>();

        var act = async () =>
        {
            await uow.TransactionAsync(async () =>
            {
                var r = await ef.Table.DeleteAsync(2.ToGuid()).ConfigureAwait(false);
                r.WasMutated.Should().BeTrue();    // I.e. was deleted successfully!

                throw new InvalidOperationException("Stop!");
            }).ConfigureAwait(false);
        };

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Stop!").ConfigureAwait(false);

        // Confirm the record is still there!
        dc.ChangeTracker.Clear();
        var m = await ef.Table.GetAsync(2.ToGuid()).ConfigureAwait(false);
        m.Should().NotBeNull();

    }).AssertSuccess());

    [Test]
    public void Rollback_On_Error() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();
        var uow = ExecutionContext.GetRequiredService<IUnitOfWork>();
        var dc = ExecutionContext.GetRequiredService<TestDbContext>();

        var r = await uow.TransactionAsync(async () =>
        {
            var r = await ef.Table.DeleteAsync(2.ToGuid()).ConfigureAwait(false);
            r.WasMutated.Should().BeTrue();    // I.e. was deleted successfully!

            return Result.ConflictError("Stop!");
        }).ConfigureAwait(false);

        r.IsConflictError.Should().BeTrue();
        r.Error.Message.Should().Be("Stop!");

        // Confirm the record is still there!
        dc.ChangeTracker.Clear();
        var m = await ef.Table.GetAsync(2.ToGuid()).ConfigureAwait(false);
        m.Should().NotBeNull();

    }).AssertSuccess());

    [Test]
    public void Commit_Success() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();
        var uow = ExecutionContext.GetRequiredService<IUnitOfWork>();
        var dc = ExecutionContext.GetRequiredService<TestDbContext>();

        await uow.TransactionAsync(async () =>
        {
            var r = await ef.Table.DeleteAsync(8.ToGuid()).ConfigureAwait(false);
            r.WasMutated.Should().BeTrue();    // I.e. was deleted successfully!
        }).ConfigureAwait(false);

        // Confirm the record is gone!
        dc.ChangeTracker.Clear();
        var m = await ef.Table.GetAsync(8.ToGuid()).ConfigureAwait(false);
        m.Should().BeNull();
    }).AssertSuccess());

    [Test]
    public void Commit_Success_Result() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();
        var uow = ExecutionContext.GetRequiredService<IUnitOfWork>();
        var dc = ExecutionContext.GetRequiredService<TestDbContext>();

        await uow.TransactionAsync(async () =>
        {
            var r = await ef.Table.DeleteAsync(9.ToGuid()).ConfigureAwait(false);
            r.WasMutated.Should().BeTrue();    // I.e. was deleted successfully!
            return Result.Success;
        }).ConfigureAwait(false);

        // Confirm the record is gone!
        dc.ChangeTracker.Clear();
        var m = await ef.Table.GetAsync(9.ToGuid()).ConfigureAwait(false);
        m.Should().BeNull();
    }).AssertSuccess());
}