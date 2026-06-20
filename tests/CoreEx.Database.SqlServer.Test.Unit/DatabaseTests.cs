using CoreEx.Database.SqlServer.Test.Unit.Models;

namespace CoreEx.Database.SqlServer.Test.Unit;

public class DatabaseTests : DatabaseTestBase
{
    [Test]
    public void SelectAndGetNullValues() => Test.ScopedType<SqlServerDatabase>(test =>
    {
        test.Run(async db =>
        {
            var tt = await db.Statement("SELECT * FROM [Test].[Table] WHERE [TableId] = @Id").Param("Id", 1.ToGuid()).SelectSingleAsync(new TestTableMapper()).ConfigureAwait(false);

            tt.Should().NotBeNull();
            tt.Id.Should().Be(1.ToGuid());
            tt.Text.Should().BeNull();
            tt.Number.Should().BeNull();
            tt.Amount.Should().BeNull();
            tt.Flag.Should().BeNull();
            tt.Date.Should().BeNull();
            tt.Time.Should().BeNull();
            tt.Json.Should().BeNull();
            tt.ETag.Should().NotBeNull();
            tt.CreatedBy.Should().NotBeNull();
            tt.CreatedOn.Should().NotBeNull();
            tt.UpdatedBy.Should().BeNull();
            tt.UpdatedOn.Should().BeNull();
        }).AssertSuccess();
    });

    [Test]
    public void SelectAndGetValues() => Test.ScopedType<SqlServerDatabase>(test =>
    {
        test.Run(async db =>
        {
            var tt = await db.Statement("SELECT * FROM [Test].[Table] WHERE [TableId] = @Id").Param("Id", 2.ToGuid()).SelectSingleAsync(new TestTableMapper()).ConfigureAwait(false);

            tt.Should().NotBeNull();
            tt.Id.Should().Be(2.ToGuid());
            tt.Text.Should().Be("Abc");
            tt.Number.Should().Be(123);
            tt.Amount.Should().Be(45.67m);
            tt.Flag.Should().BeTrue();
            tt.Date.Should().Be(new DateOnly(2024, 6, 20));
            tt.Time.Should().Be(new TimeOnly(14, 30, 59));
            tt.ETag.Should().NotBeNull();
            tt.Json.Should().NotBeNull().And.Subject.ToString().Should().Be("{\"Key\": \"Value\"}");
            tt.CreatedBy.Should().NotBeNull();
            tt.CreatedOn.Should().NotBeNull();
            tt.UpdatedBy.Should().BeNull();
            tt.UpdatedOn.Should().BeNull();
        }).AssertSuccess();
    });

    [Test]
    public void SelectSingleAsync() => Test.ScopedType<SqlServerDatabase>(test =>
    {
        test.Run(async db =>
        {
            // Exactly one row.
            var tt = await db.Statement("SELECT * FROM [Test].[Table] WHERE [TableId] = @Id").Param("Id", 2.ToGuid()).SelectSingleAsync(new TestTableMapper()).ConfigureAwait(false);
            tt.Should().NotBeNull();

            // More than one row.
            var act = () => db.Statement("SELECT * FROM [Test].[Table]").SelectSingleAsync(new TestTableMapper());
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("SelectSingleAsync has returned more than one row.");

            // No rows.
            var act2 = () => db.Statement("SELECT * FROM [Test].[Table ]WHERE [TableId] = @Id").Param("Id", 404.ToGuid()).SelectSingleAsync(new TestTableMapper());
            await act2.Should().ThrowAsync<InvalidOperationException>().WithMessage("SelectSingleAsync has not returned a row.");

        }).AssertSuccess();
    });

    [Test]
    public void SelectSingleOrDefaultAsync() => Test.ScopedType<SqlServerDatabase>(test =>
    {
        test.Run(async db =>
        {
            // Exactly one row.
            var tt = await db.Statement("SELECT * FROM [Test].[Table] WHERE [TableId] = @Id").Param("Id", 2.ToGuid()).SelectSingleOrDefaultAsync(new TestTableMapper()).ConfigureAwait(false);
            tt.Should().NotBeNull();

            // More than one row.
            var act = () => db.Statement("SELECT * FROM [Test].[Table]").SelectSingleOrDefaultAsync(new TestTableMapper());
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("SelectSingleOrDefaultAsync has returned more than one row.");

            // No rows.
            var tt2 = await db.Statement("SELECT * FROM [Test].[Table ]WHERE [TableId] = @Id").Param("Id", 404.ToGuid()).SelectSingleOrDefaultAsync(new TestTableMapper()).ConfigureAwait(false);
            tt2.Should().BeNull();
        }).AssertSuccess();
    });

    [Test]
    public void SelectFirstAsync() => Test.ScopedType<SqlServerDatabase>(test =>
    {
        test.Run(async db =>
        {
            // Exactly one row.
            var tt = await db.Statement("SELECT * FROM [Test].[Table] WHERE [TableId] = @Id").Param("Id", 2.ToGuid()).SelectFirstAsync(new TestTableMapper()).ConfigureAwait(false);
            tt.Should().NotBeNull();

            // More than one row.
            var tt2 = await db.Statement("SELECT * FROM [Test].[Table]").SelectFirstAsync(new TestTableMapper()).ConfigureAwait(false);
            tt2.Should().NotBeNull();

            // No rows.
            var act = () => db.Statement("SELECT * FROM [Test].[Table ]WHERE [TableId] = @Id").Param("Id", 404.ToGuid()).SelectFirstAsync(new TestTableMapper());
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("SelectFirstAsync has not returned a row.");
        }).AssertSuccess();
    });

    [Test]
    public void SelectFirstOrDefaultAsync() => Test.ScopedType<SqlServerDatabase>(test =>
    {
        test.Run(async db =>
        {
            // Exactly one row.
            var tt = await db.Statement("SELECT * FROM [Test].[Table] WHERE [TableId] = @Id").Param("Id", 2.ToGuid()).SelectFirstOrDefaultAsync(new TestTableMapper()).ConfigureAwait(false);
            tt.Should().NotBeNull();

            // More than one row.
            var tt2 = await db.Statement("SELECT * FROM [Test].[Table]").SelectFirstOrDefaultAsync(new TestTableMapper()).ConfigureAwait(false);
            tt2.Should().NotBeNull();

            // No rows.
            var tt3 = await db.Statement("SELECT * FROM [Test].[Table] WHERE [TableId] = @Id").Param("Id", 404.ToGuid()).SelectFirstOrDefaultAsync(new TestTableMapper()).ConfigureAwait(false);
            tt3.Should().BeNull();
        }).AssertSuccess();
    });

    [Test]
    public void SelectQueryAsync() => Test.ScopedType<SqlServerDatabase>(test =>
    {
        test.Run(async db =>
        {
            var tts = await db.Statement("SELECT * FROM [Test].[Table]").SelectQueryAsync(new TestTableMapper()).ConfigureAwait(false);
            tts.Should().NotBeNull().And.HaveCount(10);

            var ttc = new List<TestTable>();
            await db.Statement("SELECT * FROM [Test].[Table]").SelectQueryAsync(ttc, new TestTableMapper()).ConfigureAwait(false);
            ttc.Should().NotBeNull().And.HaveCount(10);
        }).AssertSuccess();
    });

    [Test]
    public void SelectAsync() => Test.ScopedType<SqlServerDatabase>(test =>
    {
        test.Run(async db =>
        {
            var count = 0;
            await db.Statement("SELECT * FROM [Test].[Table]").SelectAsync(r =>
            {
                count++;
                return count < 3;
            }).ConfigureAwait(false);

            count.Should().Be(3);
        }).AssertSuccess();
    });

    [Test]
    public void ScalarAsync() => Test.ScopedType<SqlServerDatabase>(test =>
    {
        test.Run(async db =>
        {
            var count = await db.Statement("SELECT COUNT(*) FROM [Test].[Table]").ScalarAsync<int>().ConfigureAwait(false);
            count.Should().Be(10);

            var created = await db.Statement("SELECT TOP 1 [CreatedOn] FROM [Test].[Table] WHERE [TableId] = @Id").Param("Id", 1.ToGuid()).ScalarAsync<DateTimeOffset?>().ConfigureAwait(false);
            created.Should().NotBeNull();

            created = await db.Statement("SELECT TOP 1 [CreatedOn] FROM [Test].[Table] WHERE [TableId] = @Id").Param("Id", 404.ToGuid()).ScalarAsync<DateTimeOffset?>().ConfigureAwait(false);
            created.Should().BeNull();

        }).AssertSuccess();
    });

    [Test]
    public void NonQueryAsync() => Test.ScopedType<SqlServerDatabase>(test =>
    {
        test.Run(async db =>
        {
            var rows = await db.Statement("UPDATE [Test].[Table] SET [Number] = @Number + 1 WHERE [TableId] = @Id").Param("Id", 4.ToGuid()).Param("Number", 88).NonQueryAsync().ConfigureAwait(false);
            rows.Should().Be(1);

            var number = await db.Statement("SELECT [Number] FROM [Test].[Table] WHERE [TableId] = @Id").Param("Id", 4.ToGuid()).ScalarAsync<int?>().ConfigureAwait(false);
            number.Should().Be(89);
        }).AssertSuccess();
    });
}