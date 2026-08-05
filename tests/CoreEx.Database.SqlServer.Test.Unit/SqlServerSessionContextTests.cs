namespace CoreEx.Database.SqlServer.Test.Unit;

public class SqlServerSessionContextTests : DatabaseTestBase
{
    [Test]
    public void SetSqlSessionContextAsync_SetsAllValues() => Test.ScopedType<SqlServerDatabase>(test =>
    {
        test.Run(async db =>
        {
            await db.SetSqlSessionContextAsync("test-user", DateTimeOffset.UtcNow, "tenant-x", "user-123").ConfigureAwait(false);

            (await GetSessionContextAsync(db, "Username").ConfigureAwait(false)).Should().Be("test-user");
            (await GetSessionContextAsync(db, "TenantId").ConfigureAwait(false)).Should().Be("tenant-x");
            (await GetSessionContextAsync(db, "UserId").ConfigureAwait(false)).Should().Be("user-123");
        }).AssertSuccess();
    });

    // Regression coverage for the ParamWith cast fix: tenantId/userId are added only "with" a non-default value; when omitted they must not be passed at all (remaining unset in SESSION_CONTEXT), not fail with a cast exception.
    [Test]
    public void SetSqlSessionContextAsync_OmittedTenantAndUser_LeavesSessionContextUnset() => Test.ScopedType<SqlServerDatabase>(test =>
    {
        test.Run(async db =>
        {
            await db.SetSqlSessionContextAsync("only-user", DateTimeOffset.UtcNow).ConfigureAwait(false);

            (await GetSessionContextAsync(db, "Username").ConfigureAwait(false)).Should().Be("only-user");
            (await GetSessionContextAsync(db, "TenantId").ConfigureAwait(false)).Should().BeNull();
            (await GetSessionContextAsync(db, "UserId").ConfigureAwait(false)).Should().BeNull();
        }).AssertSuccess();
    });

    private static Task<string?> GetSessionContextAsync(SqlServerDatabase db, string key)
        => db.Statement($"SELECT CAST(SESSION_CONTEXT(N'{key}') AS NVARCHAR(250))").ScalarAsync<string?>();
}
