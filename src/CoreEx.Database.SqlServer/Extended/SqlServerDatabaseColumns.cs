namespace CoreEx.Database.SqlServer.Extended;

/// <summary>
/// Extends the <see cref="DatabaseColumns"/> configuring/adding SQL Server specific columns.
/// </summary>
public record class SqlServerDatabaseColumns : DatabaseColumns
{
    /// <summary>
    /// Gets or sets the default <see cref="SqlServerDatabaseColumns"/>.
    /// </summary>
    public static SqlServerDatabaseColumns Default { get; set; } = new SqlServerDatabaseColumns();

    /// <summary>
    /// Gets or sets the session context '<c>Username</c>' column name.
    /// </summary>
    public string SessionContextUsernameName { get; set; } = "Username";

    /// <summary>
    /// Gets or sets the session context '<c>Timestamp</c>' column name.
    /// </summary>
    public string SessionContextTimestampName { get; set; } = "Timestamp";

    /// <summary>
    /// Gets or sets the '<c>TenantId</c>' column name.
    /// </summary>
    public string SessionContextTenantIdName { get; set; } = "TenantId";

    /// <summary>
    /// Gets or sets the session context '<c>UserId</c>' column name.
    /// </summary>
    public string SessionContextUserIdName { get; set; } = "UserId";
}