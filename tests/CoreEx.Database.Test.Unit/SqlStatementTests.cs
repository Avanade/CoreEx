namespace CoreEx.Database.Test.Unit;

[TestFixture]
public class SqlStatementTests
{
    [Test]
    public void IsIndeterminate()
    {
        SqlStatement.Indeterminate.IsIndeterminate.Should().BeTrue();
        SqlStatement.FromText("SELECT * FROM TABLE").IsIndeterminate.Should().BeFalse();
        SqlStatement.StoredProcedure("SP_TEST").IsIndeterminate.Should().BeFalse();
    }
}