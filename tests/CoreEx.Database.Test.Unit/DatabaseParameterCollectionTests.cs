using CoreEx.Database.SqlServer;
using CoreEx.Mapping.Converters;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CoreEx.Database.Test.Unit;

[TestFixture]
public class DatabaseParameterCollectionTests
{
    [Test]
    public void AddParameter_Null()
    {
        var dp = CreateCollection().AddParameter("foo");
        dp.ParameterName.Should().Be("@foo");
        dp.DbType.Should().Be(System.Data.DbType.String);
        dp.Value.Should().Be(DBNull.Value);
        dp.Direction.Should().Be(System.Data.ParameterDirection.Input);
    }

    [Test]
    public void AddParameter_WithValue()
    {
        var dp = CreateCollection().AddParameter("bar", 123);
        dp.ParameterName.Should().Be("@bar");
        dp.Value.Should().Be(123);
        dp.Direction.Should().Be(ParameterDirection.Input);
    }

    [Test]
    public void AddParameter_WithDbType()
    {
        var dp = CreateCollection().AddParameter("baz", 456, DbType.Int32, ParameterDirection.Output);
        dp.ParameterName.Should().Be("@baz");
        dp.Value.Should().Be(456);
        dp.DbType.Should().Be(DbType.Int32);
        dp.Direction.Should().Be(ParameterDirection.Output);
    }

    [Test]
    public void AddParameter_WithSize()
    {
        var dp = CreateCollection().AddParameter("qux", DbType.String, 50, ParameterDirection.Input);
        dp.ParameterName.Should().Be("@qux");
        dp.DbType.Should().Be(DbType.String);
        dp.Size.Should().Be(50);
        dp.Direction.Should().Be(ParameterDirection.Input);
    }

    [Test]
    public void AddParameter_DateTimeOffset()
    {
        var dto = new DateTimeOffset(2020, 01, 08, 06, 30, 59, TimeSpan.FromHours(8));
        var dtoUtc = dto.ToUniversalTime();
        var dp = CreateCollection().AddParameter("dto", dto);
        dp.ParameterName.Should().Be("@dto");
        dp.Value.Should().Be(dtoUtc);

        // Cast it back and make sure it's UTC.
        var dtoUtcCast = (DateTimeOffset)dp.Value;
        dtoUtcCast.Offset.Should().Be(TimeSpan.Zero);
    }

    [Test]
    public void AddJsonParameter()
    {
        var obj = new { X = 1, Y = "abc" };
        var dp = CreateCollection().AddJsonParameter("json", obj);
        dp.ParameterName.Should().Be("@json");
        dp.Value.Should().Be(System.Text.Json.JsonSerializer.Serialize(obj, CreateDatabase().JsonSerializerOptions));
    }

    [Test]
    public void AddWildcardParameter()
    {
        var dp = CreateCollection().AddWildcardParameter("wild", "a*b?c");
        dp.ParameterName.Should().Be("@wild");
        dp.Value.Should().Be("a%b_c");

        var dp2 = CreateCollection().AddWildcardParameter("wildx", "a%b_c");
        dp2.ParameterName.Should().Be("@wildx");
        dp2.Value.Should().Be("a[%]b[_]c");
    }

    [Test]
    public void AddReturnValueParameter()
    {
        var dp = CreateCollection().AddReturnValueParameter();
        dp.ParameterName.Should().Be("@ReturnValue");
        dp.DbType.Should().Be(DbType.Int32);
        dp.Direction.Should().Be(ParameterDirection.ReturnValue);
    }

    [Test]
    public void AddReselectRecordParam()
    {
        var dp = CreateCollection().AddReselectRecordParam();
        dp.ParameterName.Should().Be("@ReselectRecord");
        dp.Value.Should().Be(true);
    }

    [Test]
    public void AddRowVersionParam()
    {
        var rv = StringBase64Converter.Default.ConvertToDestination("12345678");
        var dp = CreateCollection().AddRowVersionParam("12345678");
        dp.ParameterName.Should().Be("@RowVersion");
        dp.Value.Should().BeEquivalentTo(rv);
    }

    [Test]
    public void ParameterizeName_AddsAtIfMissing()
    {
        DatabaseParameterCollection.ParameterizeName("foo").Should().Be("@foo");
        DatabaseParameterCollection.ParameterizeName("@bar").Should().Be("@bar");
    }

    [Test]
    public void Add_And_Remove_Parameter()
    {
        var collection = CreateCollection();
        var param = collection.AddParameter("foo", 1);
        collection.Contains(param).Should().BeTrue();
        collection.Remove(param).Should().BeTrue();
        collection.Contains(param).Should().BeFalse();
    }

    [Test]
    public void Clear_RemovesAllParameters()
    {
        var collection = CreateCollection();
        collection.AddParameter("foo", 1);
        collection.AddParameter("bar", 2);
        collection.Count.Should().Be(2);
        collection.Clear();
        collection.Count.Should().Be(0);
    }

    [Test]
    public void Indexer_ReturnsParameter()
    {
        var collection = CreateCollection();
        var param = collection.AddParameter("foo", 1);
        collection[0].Should().Be(param);
    }

    /* Extension Methods */

    [Test]
    public void ExtensionMethods_Param()
    {
        var collection = CreateCollection()
            .Param("abc")
            .Param("foo", 1)
            .Param("bar", "abc", DbType.String, ParameterDirection.Output)
            .Param("baz", DbType.Int32, 10, ParameterDirection.Input)
            .JsonParam("json", new { X = 1, Y = "abc" })
            .WildCardParam("wild", "*")
            .RowVersionParam("12345678")
            .ReselectRecordParam(true);

        collection.Count.Should().Be(8);
        collection[0].ParameterName.Should().Be("@abc");
        collection[1].ParameterName.Should().Be("@foo");
        collection[2].ParameterName.Should().Be("@bar");
        collection[3].ParameterName.Should().Be("@baz");
        collection[4].ParameterName.Should().Be("@json");
        collection[5].ParameterName.Should().Be("@wild");
        collection[6].ParameterName.Should().Be("@RowVersion");
        collection[7].ParameterName.Should().Be("@ReselectRecord");
    }

    [Test]
    public void ExtensionMethods_ParamWith()
    {
        var collection = CreateCollection()
            .ParamWhen(false, "foo", () => 1)
            .ParamWhen(true, "fop", () => 1)
            .ParamWhen(false, "bar", () => "abc", DbType.String, ParameterDirection.Output)
            .ParamWhen(true, "baz", () => 10, DbType.Int32, ParameterDirection.Input)
            .JsonParamWhen(false, "json", () => new { X = 1, Y = "abc" })
            .JsonParamWhen(true, "jsonx", () => new { X = 1, Y = "abc" })
            .WildcardParamWhen(false, "wild", () => "*")
            .WildcardParamWhen(true, "wildx", () => "*");

        collection.Count.Should().Be(4);
        collection[0].ParameterName.Should().Be("@fop");
        collection[1].ParameterName.Should().Be("@baz");
        collection[2].ParameterName.Should().Be("@jsonx");
        collection[3].ParameterName.Should().Be("@wildx");

        collection = CreateCollection().RowVersionParamWhen(false, "12345678");
        collection.Count.Should().Be(0);

        collection = CreateCollection().RowVersionParamWhen(true, "12345678");
        collection.Count.Should().Be(1);
        collection[0].ParameterName.Should().Be("@RowVersion");
    }

    [Test]
    public void ExtensionMethods_ParamWhen_Itself()
    {
        var collection = CreateCollection()
            .ParamWith((string?)null, "foo")
            .ParamWith("abc", "fop")
            .ParamWith(0, "bar", direction: ParameterDirection.Input)
            .ParamWith(1, "baz", direction: ParameterDirection.InputOutput)
            .JsonParamWith((string?)null, "json")
            .JsonParamWith("xyz", "jsonx")
            .WildcardParamWith(null, "wild")
            .WildcardParamWith("abc", "wildx");

        collection.Count.Should().Be(4);
        collection[0].ParameterName.Should().Be("@fop");
        collection[1].ParameterName.Should().Be("@baz");
        collection[2].ParameterName.Should().Be("@jsonx");
        collection[3].ParameterName.Should().Be("@wildx");

        collection = CreateCollection().RowVersionParamWith(null);
        collection.Count.Should().Be(0);

        collection = CreateCollection().RowVersionParamWith("12345678");
        collection.Count.Should().Be(1);
        collection[0].ParameterName.Should().Be("@RowVersion");
    }

    [Test]
    public void ExtensionMethods_ParamWhen_Value()
    {
        var collection = CreateCollection()
            .ParamWith((string?)null, "foo", () => 1)
            .ParamWith("abc", "fop", () => 1)
            .ParamWith(0, "bar", () => 1)
            .ParamWith(1, "baz", () => 1)
            .JsonParamWith((string?)null, "json", () => 1)
            .JsonParamWith("xyz", "jsonx", () => 1)
            .WildcardParamWith((string?)null, "wild", () => "*")
            .WildcardParamWith("xyz", "wildx", () => "*");

        collection.Count.Should().Be(4);
        collection[0].ParameterName.Should().Be("@fop");
        collection[1].ParameterName.Should().Be("@baz");
        collection[2].ParameterName.Should().Be("@jsonx");
        collection[3].ParameterName.Should().Be("@wildx");
    }

    [Test]
    public void ExtensionMethods_PagingParams()
    {
        var collection = CreateCollection().PagingParams(null);
        collection.Count.Should().Be(0);

        collection = CreateCollection().PagingParams(new Data.PagingArgs(10, 5));
        collection.Count.Should().Be(2);
        collection[0].ParameterName.Should().Be("@PagingSkip");
        collection[0].Value.Should().Be(10);
        collection[1].ParameterName.Should().Be("@PagingTake");
        collection[1].Value.Should().Be(5);

        collection = CreateCollection().PagingParams(new Data.PagingArgs(8, 4, true));
        collection.Count.Should().Be(3);
        collection[0].ParameterName.Should().Be("@PagingSkip");
        collection[0].Value.Should().Be(8);
        collection[1].ParameterName.Should().Be("@PagingTake");
        collection[1].Value.Should().Be(4);
        collection[2].ParameterName.Should().Be("@PagingCount");
        collection[2].Value.Should().Be(true);
    }

    /* Utility */

    private static SqlServerDatabase CreateDatabase() => new((SqlConnection)SqlClientFactory.Instance.CreateConnection())
    {
        Wildcard = new Extended.DatabaseWildcard(Wildcards.Wildcard.BothAll)
    };

    private static DatabaseParameterCollection CreateCollection() => CreateDatabase().Statement(SqlStatement.FromText("SELECT 1")).Parameters;
}