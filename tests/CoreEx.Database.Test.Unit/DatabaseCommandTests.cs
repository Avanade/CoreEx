using CoreEx.Database.SqlServer;
using Microsoft.Data.SqlClient;

namespace CoreEx.Database.Test.Unit;

[TestFixture]
public class DatabaseCommandTests
{
    [Test]
    public async Task NonQueryAsync_IndeterminateStatement_ThrowsBeforeOpeningConnection()
    {
        var database = new SqlServerDatabase((SqlConnection)SqlClientFactory.Instance.CreateConnection());
        Func<Task> act = () => database.Statement(SqlStatement.Indeterminate).NonQueryAsync();

        await act.Should().ThrowAsync<InvalidOperationException>().ConfigureAwait(false);

        // The connection must never have been opened; the guard fires before any database I/O is attempted.
        ((IDatabase)database).Connection.State.Should().Be(System.Data.ConnectionState.Closed);
    }
}
