// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Database.Extended;
using CoreEx.Entities;

namespace CoreEx.Database.Postgres
{
    /// <summary>
    /// Extends the <see cref="DatabaseColumns"/> adding additional SQL Server specific.
    /// </summary>
    /// <remarks>Overrides the <see cref="DatabaseColumns.RowVersionName"/> to '<c>xmin</c>'. This is a PostgreSQL system column (hidden); see <see href="https://www.postgresql.org/docs/current/ddl-system-columns.html#DDL-SYSTEM-COLUMNS"/> 
    /// and <see href="https://www.npgsql.org/efcore/modeling/concurrency.html"/> for more information.</remarks>
    public class PostgresDatabaseColumns : DatabaseColumns
    {
        /// <summary>
        /// Gets or sets the session context '<c>Username</c>' column name.
        /// </summary>
        public string SessionContextUsernameName { get; set; } = "Username";

        /// <summary>
        /// Gets or sets the session context '<c>Timestamp</c>' column name.
        /// </summary>
        public string SessionContextTimestampName { get; set; } = "Timestamp";

        /// <summary>
        /// Gets or sets the <see cref="ITenantId.TenantId"/> column name.
        /// </summary>
        public string SessionContextTenantIdName { get; set; } = "TenantId";

        /// <summary>
        /// Gets or sets the session context '<c>UserId</c>' column name.
        /// </summary>
        public string SessionContextUserIdName { get; set; } = "UserId";

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgresDatabaseColumns"/> class.
        /// </summary>
        public PostgresDatabaseColumns() => RowVersionName = "xmin";
    }
}