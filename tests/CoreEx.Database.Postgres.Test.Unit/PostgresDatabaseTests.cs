using Npgsql;

namespace CoreEx.Database.Postgres.Test.Unit;

[TestFixture]
public class PostgresDatabaseTests
{
    // Regression test: the constructor used to call dataSource.CreateConnection() directly in the base-constructor
    // argument list, so a null dataSource surfaced as a raw NullReferenceException instead of ArgumentNullException.
    [Test]
    public void Constructor_NullDataSource_ThrowsArgumentNullException()
    {
        NpgsqlDataSource dataSource = null!;
        Action act = () => new PostgresDatabase(dataSource);
        act.Should().Throw<ArgumentNullException>().WithParameterName("dataSource");
    }

    [TestCase("56001", typeof(ValidationException))]
    [TestCase("56002", typeof(BusinessException))]
    [TestCase("56003", typeof(AuthorizationException))]
    [TestCase("56004", typeof(ConcurrencyException))]
    [TestCase("56005", typeof(NotFoundException))]
    [TestCase("56006", typeof(ConflictException))]
    [TestCase("56007", typeof(DuplicateException))]
    [TestCase("56010", typeof(DataConsistencyException))]
    public void OnDbException_MapsKnownSqlStateToSemanticException(string sqlState, Type expectedType)
    {
        var pex = new PostgresException("Test message.", "ERROR", "ERROR", sqlState);
        var hex = ((IDatabase)CreateDatabase()).HandleDbException(pex);

        hex.Should().NotBeNull();
        hex.Should().BeOfType(expectedType);
        hex!.InnerException.Should().BeSameAs(pex);
        hex.Message.Should().Be(pex.Message.TrimEnd());
    }

    [Test]
    public void OnDbException_DefaultDuplicateSqlState_MapsToDuplicateException()
    {
        var pex = new PostgresException("Unique violation.", "ERROR", "ERROR", "23505");
        var hex = ((IDatabase)CreateDatabase()).HandleDbException(pex);

        hex.Should().BeOfType<DuplicateException>();
        hex!.InnerException.Should().BeSameAs(pex);
    }

    [Test]
    public void OnDbException_CheckDuplicateErrorNumbersDisabled_DoesNotMapDuplicateSqlState()
    {
        var db = CreateDatabase();
        db.CheckDuplicateErrorNumbers = false;

        var pex = new PostgresException("Unique violation.", "ERROR", "ERROR", "23505");
        var hex = ((IDatabase)db).HandleDbException(pex);

        hex.Should().BeNull();
    }

    [Test]
    public void OnDbException_UnmappedSqlState_ReturnsNull()
    {
        var pex = new PostgresException("Some other error.", "ERROR", "ERROR", "42601");
        var hex = ((IDatabase)CreateDatabase()).HandleDbException(pex);

        hex.Should().BeNull();
    }

    private static PostgresDatabase CreateDatabase() => new(NpgsqlDataSource.Create("Host=localhost;Database=dummy;Username=dummy;Password=dummy"));
}
