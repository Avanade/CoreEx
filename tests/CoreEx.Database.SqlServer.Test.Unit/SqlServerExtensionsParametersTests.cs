using Microsoft.Data.SqlClient;
using System.Data;

namespace CoreEx.Database.SqlServer.Test.Unit;

[TestFixture]
public class SqlServerExtensionsParametersTests
{
    [Test]
    public void AddParameter_WithSqlDbType()
    {
        var dp = CreateCollection().AddParameter("foo", 123, SqlDbType.Int);
        dp.ParameterName.Should().Be("@foo");
        dp.Value.Should().Be(123);
        dp.SqlDbType.Should().Be(SqlDbType.Int);
        dp.Direction.Should().Be(ParameterDirection.Input);
    }

    [Test]
    public void Param_AddsParameter()
    {
        var collection = CreateCollection().Param("foo", 123, SqlDbType.Int);
        collection.Count.Should().Be(1);
        collection[0].ParameterName.Should().Be("@foo");
    }

    [Test]
    public void ParamWhen_OnlyAddsWhenTrue()
    {
        var collection = CreateCollection()
            .ParamWhen(false, "foo", () => 1, SqlDbType.Int)
            .ParamWhen(true, "bar", () => 2, SqlDbType.Int);

        collection.Count.Should().Be(1);
        collection[0].ParameterName.Should().Be("@bar");
    }

    // Regression test: ParamWith<TSelf, T>(object? with, ...) used to force-cast the boxed `with` value to the
    // value's own type T via `(T)with`, throwing InvalidCastException whenever the "check" type (here, Guid)
    // differed from the parameter's value type (here, string). Fixed by using independent TWith/TValue generics,
    // mirroring the base CoreEx.Database.ParamWith<TSelf, TWith, TValue> convention.
    [Test]
    public void ParamWith_MismatchedCheckAndValueTypes_DoesNotThrow()
    {
        var tenantId = Guid.NewGuid();
        Action act = () => CreateCollection().ParamWith(tenantId, "tenant", () => tenantId.ToString(), SqlDbType.NVarChar);
        act.Should().NotThrow();

        var collection = CreateCollection().ParamWith(tenantId, "tenant", () => tenantId.ToString(), SqlDbType.NVarChar);
        collection.Count.Should().Be(1);
        collection[0].ParameterName.Should().Be("@tenant");
        collection[0].Value.Should().Be(tenantId.ToString());
    }

    [Test]
    public void ParamWith_DefaultCheckValue_DoesNotAddParameter()
    {
        var collection = CreateCollection().ParamWith(Guid.Empty, "tenant", () => "abc", SqlDbType.NVarChar);
        collection.Count.Should().Be(0);
    }

    [Test]
    public void ParamWith_SameCheckAndValueType_StillWorks()
    {
        var collection = CreateCollection().ParamWith("abc", "foo", null, SqlDbType.NVarChar);
        collection.Count.Should().Be(1);
        collection[0].ParameterName.Should().Be("@foo");
        collection[0].Value.Should().Be("abc");
    }

    private static SqlServerDatabase CreateDatabase() => new((SqlConnection)SqlClientFactory.Instance.CreateConnection());

    private static DatabaseParameterCollection CreateCollection() => CreateDatabase().Statement(SqlStatement.FromText("SELECT 1")).Parameters;
}
