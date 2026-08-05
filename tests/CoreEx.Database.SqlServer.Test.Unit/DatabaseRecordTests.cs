namespace CoreEx.Database.SqlServer.Test.Unit;

public class DatabaseRecordTests : DatabaseTestBase
{
    [Test]
    public void TryGetValue_And_GetValueOrDefault() => Test.ScopedType<SqlServerDatabase>(test =>
    {
        test.Run(async db =>
        {
            await db.Statement("SELECT * FROM [Test].[Table] WHERE [TableId] = @Id").Param("Id", 2.ToGuid()).SelectAsync(r =>
            {
                r.TryGetValue<string?>("Text", out var text).Should().BeTrue();
                text.Should().Be("Abc");

                r.TryGetValue<string?>("DoesNotExist", out var missing).Should().BeFalse();
                missing.Should().BeNull();

                r.GetValueOrDefault<int?>("Number").Should().Be(123);
                r.GetValueOrDefault<int?>("DoesNotExist", 999).Should().Be(999);

                return false;
            }).ConfigureAwait(false);
        }).AssertSuccess();
    });

    [Test]
    public void TryGetOrdinal_FoundAndNotFound() => Test.ScopedType<SqlServerDatabase>(test =>
    {
        test.Run(async db =>
        {
            await db.Statement("SELECT * FROM [Test].[Table] WHERE [TableId] = @Id").Param("Id", 2.ToGuid()).SelectAsync(r =>
            {
                r.TryGetOrdinal("Text", out var ordinal).Should().BeTrue();
                ordinal.Should().BeGreaterThanOrEqualTo(0);

                r.TryGetOrdinal("DoesNotExist", out _).Should().BeFalse();

                return false;
            }).ConfigureAwait(false);
        }).AssertSuccess();
    });

    [Test]
    public void IsDBNull_ForNullAndNonNullColumns() => Test.ScopedType<SqlServerDatabase>(test =>
    {
        test.Run(async db =>
        {
            // TableId 1 has no Text/Number/etc. set (see Data\data.yaml).
            await db.Statement("SELECT * FROM [Test].[Table] WHERE [TableId] = @Id").Param("Id", 1.ToGuid()).SelectAsync(r =>
            {
                r.IsDBNull("Text", out var textOrdinal).Should().BeTrue();
                textOrdinal.Should().BeGreaterThanOrEqualTo(0);

                r.IsDBNull("CreatedBy", out _).Should().BeFalse();

                return false;
            }).ConfigureAwait(false);
        }).AssertSuccess();
    });

    [Test]
    public void GetRowVersion_ReturnsNonEmptyEncodedValue() => Test.ScopedType<SqlServerDatabase>(test =>
    {
        test.Run(async db =>
        {
            await db.Statement("SELECT * FROM [Test].[Table] WHERE [TableId] = @Id").Param("Id", 2.ToGuid()).SelectAsync(r =>
            {
                r.GetRowVersion().Should().NotBeNullOrEmpty();
                return false;
            }).ConfigureAwait(false);
        }).AssertSuccess();
    });

    [Test]
    public void GetValueFromJson_DeserializesJsonColumn() => Test.ScopedType<SqlServerDatabase>(test =>
    {
        test.Run(async db =>
        {
            await db.Statement("SELECT * FROM [Test].[Table] WHERE [TableId] = @Id").Param("Id", 2.ToGuid()).SelectAsync(r =>
            {
                var kvp = r.GetValueFromJson<Dictionary<string, string>>("KvpJson");
                kvp.Should().NotBeNull();
                kvp!["Key"].Should().Be("Value");

                return false;
            }).ConfigureAwait(false);
        }).AssertSuccess();
    });
}
