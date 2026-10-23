using CoreEx.Data;
using CoreEx.Database.Postgres.Test.Unit.Repository;
using CoreEx.Results;

namespace CoreEx.Database.Postgres.Test.Unit;

public class EntityFrameworkUowTests : DatabaseTestBase
{
    [TestCase(IsolationLevel.ReadCommitted, "read committed")]
    [TestCase(IsolationLevel.RepeatableRead, "repeatable read")]
    [TestCase(IsolationLevel.Serializable, "serializable")]
    public void Root_Uses_Explicit_Isolation(IsolationLevel isolation, string serverIsolation) => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();
        var uow = ExecutionContext.GetRequiredService<PostgresUnitOfWork>();
        var result = await uow.TransactionAsync(new PostgresDatabaseArgs { IsolationLevel = isolation }, async token =>
        {
            uow.Database.CurrentTransaction!.IsolationLevel.Should().Be(isolation);
            ef.DbContext.Database.CurrentTransaction!.GetDbTransaction().Should().BeSameAs(uow.Database.CurrentTransaction);
            return await uow.Database.Statement("SHOW transaction_isolation").ScalarAsync<string>(token).ConfigureAwait(false);
        }).ConfigureAwait(false);

        result.Should().Be(serverIsolation);
        uow.Database.CurrentTransaction.Should().BeNull();
        ef.DbContext.Database.CurrentTransaction.Should().BeNull();
    }).AssertSuccess());

    [Test]
    public void Root_Uses_Configured_Isolation() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var uow = ExecutionContext.GetRequiredService<PostgresUnitOfWork>();
        uow.Database.DbArgs = uow.Database.DbArgs with { IsolationLevel = IsolationLevel.Serializable };
        await uow.TransactionAsync(async token =>
        {
            (await uow.Database.Statement("SHOW transaction_isolation").ScalarAsync<string>(token).ConfigureAwait(false)).Should().Be("serializable");
        }).ConfigureAwait(false);
        uow.Database.CurrentTransaction.Should().BeNull();
    }).AssertSuccess());

    [TestCase(IsolationLevel.ReadCommitted, IsolationLevel.RepeatableRead)]
    [TestCase(IsolationLevel.Serializable, IsolationLevel.ReadCommitted)]
    public void Nested_Inherits_Parent_Isolation(IsolationLevel parentIsolation, IsolationLevel childIsolation) => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();
        var uow = ExecutionContext.GetRequiredService<PostgresUnitOfWork>();
        await uow.TransactionAsync(new PostgresDatabaseArgs { IsolationLevel = parentIsolation }, async token =>
        {
            var parent = uow.Database.CurrentTransaction;
            await uow.TransactionAsync(new PostgresDatabaseArgs { IsolationLevel = childIsolation }, _ =>
            {
                uow.Database.CurrentTransaction.Should().BeSameAs(parent);
                parent!.IsolationLevel.Should().Be(parentIsolation);
                ef.DbContext.Database.CurrentTransaction!.GetDbTransaction().Should().BeSameAs(parent);
                return Task.CompletedTask;
            }, token).ConfigureAwait(false);
            uow.Database.CurrentTransaction.Should().BeSameAs(parent);
            (await uow.Database.Statement("SELECT 1").ScalarAsync<int>(token).ConfigureAwait(false)).Should().Be(1);
        }).ConfigureAwait(false);
        uow.Database.CurrentTransaction.Should().BeNull();
        ef.DbContext.Database.CurrentTransaction.Should().BeNull();
    }).AssertSuccess());

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
