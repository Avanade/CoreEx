using Microsoft.Data.SqlClient;

namespace CoreEx.Database.SqlServer.Test.Unit;

public class DatabaseErrorMappingTests : DatabaseTestBase
{
    [TestCase(56001, typeof(ValidationException))]
    [TestCase(56002, typeof(BusinessException))]
    [TestCase(56003, typeof(AuthorizationException))]
    [TestCase(56004, typeof(ConcurrencyException))]
    [TestCase(56005, typeof(NotFoundException))]
    [TestCase(56006, typeof(ConflictException))]
    [TestCase(56007, typeof(DuplicateException))]
    [TestCase(56010, typeof(DataConsistencyException))]
    public void OnDbException_MapsKnownErrorNumberToSemanticException(int errorNumber, Type expectedType) => Test.ScopedType<SqlServerDatabase>(test =>
    {
        test.Run(async db =>
        {
            var act = () => db.Statement($"THROW {errorNumber}, N'Test message.', 1;").NonQueryAsync();
            var ex = await act.Should().ThrowAsync<Exception>().ConfigureAwait(false);

            ex.Which.Should().BeOfType(expectedType);
            ex.Which.Message.Should().Be("Test message.");
            ex.Which.InnerException.Should().BeOfType<SqlException>();
        }).AssertSuccess();
    });

    [Test]
    public void OnDbException_UniqueIndexViolation_MapsToDuplicateException() => Test.ScopedType<SqlServerDatabase>(test =>
    {
        test.Run(async db =>
        {
            // TableId 2 already has Text='Abc', TenantId='A' (see Data\data.yaml); the unique index on (TenantId, Text) rejects a second row with the same combination.
            var act = () => db.Statement("INSERT INTO [Test].[Table] (Text, TenantId) VALUES ('Abc', 'A');").NonQueryAsync();
            var ex = await act.Should().ThrowAsync<DuplicateException>().ConfigureAwait(false);

            ex.Which.InnerException.Should().BeOfType<SqlException>().Which.Number.Should().Be(2601);
        }).AssertSuccess();
    });

    [Test]
    public void OnDbException_UnmappedErrorNumber_PropagatesOriginalSqlException() => Test.ScopedType<SqlServerDatabase>(test =>
    {
        test.Run(async db =>
        {
            var act = () => db.Statement("THROW 56099, N'Unmapped error.', 1;").NonQueryAsync();
            var ex = await act.Should().ThrowAsync<SqlException>().ConfigureAwait(false);

            ex.Which.Number.Should().Be(56099);
        }).AssertSuccess();
    });
}
