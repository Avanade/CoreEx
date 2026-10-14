namespace CoreEx.Database.Postgres.Test.Unit;

public class EntityFrameworkSqlQueryTests : DatabaseTestBase
{
    [Test]
    public void SqlQuery_BindsValuesWithoutInterpretingSql() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();
        const string value = "O'Brien'; SELECT 99; -- {0}";
        var query = ef.SqlQuery<string>(SqlStatement.FromText("SELECT CAST({0} AS text) AS \"Value\""), value);
        (await query.SingleAsync().ConfigureAwait(false)).Should().Be(value);
    }).AssertSuccess());

    [Test]
    public void SqlQuery_ForwardsExplicitProviderParameter() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();
        var parameter = new NpgsqlParameter("Input", "named parameter");
        var query = ef.SqlQuery<string>(SqlStatement.FromText("SELECT CAST(@Input AS text) AS \"Value\""), parameter);
        (await query.SingleAsync().ConfigureAwait(false)).Should().Be("named parameter");
    }).AssertSuccess());

    [Test]
    public void SqlQuery_ComposesJoinFilterOrderingPagingAndCount() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();
        var statement = SqlStatement.FromText("SELECT n AS \"Number\" FROM generate_series(1, {0}) AS n");
        var numbers = ef.SqlQuery<NumberRow>(statement, 6);
        var query = from left in numbers
                    join right in numbers on left.Number equals right.Number
                    where left.Number > 2
                    orderby left.Number descending
                    select left.Number;

        (await query.CountAsync().ConfigureAwait(false)).Should().Be(4);
        (await query.Skip(1).Take(2).ToArrayAsync().ConfigureAwait(false)).Should().Equal(5, 4);
    }).AssertSuccess());

    [Test]
    public void SqlQuery_IsDeferredAndUsesSharedTransaction() => Test.ScopedType<ExecutionContext>(test => test.Run(async _ =>
    {
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();
        var uow = ExecutionContext.GetRequiredService<PostgresUnitOfWork>();
        var query = ef.SqlQuery<string>(SqlStatement.FromText("SELECT text AS \"Value\" FROM test.\"table\" WHERE table_id = {0}"), 1.ToGuid());
        var original = await query.SingleAsync().ConfigureAwait(false);
        Func<Task> work = () => uow.TransactionAsync(async ct =>
        {
            await ef.Database.Statement("UPDATE test.\"table\" SET text = @Text WHERE table_id = @Id")
                .Param("Text", "Visible before commit", dbType: DbType.String)
                .Param("Id", 1.ToGuid(), dbType: DbType.Guid)
                .NonQueryAsync(ct).ConfigureAwait(false);
            (await query.SingleAsync(ct).ConfigureAwait(false)).Should().Be("Visible before commit");
            var transaction = ef.DbContext.Database.CurrentTransaction ?? throw new InvalidOperationException("Missing shared transaction.");
            transaction.GetDbTransaction().Should().BeSameAs(uow.Database.CurrentTransaction);
            throw new InvalidOperationException("Rollback query test.");
        });

        await work.Should().ThrowAsync<InvalidOperationException>().WithMessage("Rollback query test.").ConfigureAwait(false);
        (await query.SingleAsync().ConfigureAwait(false)).Should().Be(original);
        ef.DbContext.Database.CurrentTransaction.Should().BeNull();
        uow.Database.CurrentTransaction.Should().BeNull();
    }).AssertSuccess());

    [Test]
    public void SqlQuery_RejectsUnsupportedStatementsAndNullArguments() => Test.ScopedType<ExecutionContext>(test => test.Run(_ =>
    {
        var ef = ExecutionContext.GetRequiredService<TestEfDb>();
        Action indeterminate = () => ef.SqlQuery<int>(SqlStatement.Indeterminate);
        Action procedure = () => ef.SqlQuery<int>(SqlStatement.StoredProcedure("test.procedure"));
        // Null-forgiving values deliberately exercise runtime argument guards.
        Action missingStatement = () => ef.SqlQuery<int>(null!);
        Action missingParameters = () => ef.SqlQuery<int>(SqlStatement.FromText("SELECT 1"), null!);
        Action missingDatabase = () => EfDbExtensions.SqlQuery<int>(null!, SqlStatement.FromText("SELECT 1"));

        indeterminate.Should().Throw<InvalidOperationException>().WithMessage("*determinate statement*");
        procedure.Should().Throw<ArgumentException>().WithParameterName("statement");
        missingStatement.Should().Throw<ArgumentNullException>().WithParameterName("statement");
        missingParameters.Should().Throw<ArgumentNullException>().WithParameterName("parameters");
        missingDatabase.Should().Throw<ArgumentNullException>().WithParameterName("database");
        return Task.CompletedTask;
    }).AssertSuccess());

    public sealed class NumberRow
    {
        public int Number { get; set; }
    }
}
