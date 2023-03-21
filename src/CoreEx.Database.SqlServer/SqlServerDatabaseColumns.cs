// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Database.Extended;
using CoreEx.Entities;
using System.Collections.Generic;
using System.Data;

namespace CoreEx.Database.SqlServer
{
    /// <summary>
    /// Extends the <see cref="DatabaseColumns"/> adding additional SQL Server specific.
    /// </summary>
    public class SqlServerDatabaseColumns : DatabaseColumns
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
        /// Gets or sets the table-value parameter type name for an <see cref="IEnumerable{String}"/>.
        /// </summary>
        public string TvpStringListTypeName { get; set; } = "[dbo].[udtNVarCharList]";

        /// <summary>
        /// Gets or sets the table-value parameter type name for an <see cref="IEnumerable{Int32}"/>.
        /// </summary>
        public string TvpInt32ListTypeName { get; set; } = "[dbo].[udtIntList]";

        /// <summary>
        /// Gets or sets the table-value parameter type name for an <see cref="IEnumerable{Int64}"/>.
        /// </summary>
        public string TvpInt64ListTypeName { get; set; } = "[dbo].[udtBigIntList]";

        /// <summary>
        /// Gets or sets the table-value parameter type name for an <see cref="IEnumerable{Guid}"/>.
        /// </summary>
        public string TvpGuidListTypeName { get; set; } = "[dbo].[udtUniqueIdentifierList]";

        /// <summary>
        /// Gets or sets the table-value parameter type name for an <see cref="IEnumerable{DateTime}"/>.
        /// </summary>
        public string TvpDateTimeListTypeName { get; set; } = "[dbo].[udtDateTime2]";

        /// <summary>
        /// Gets or sets the table-value parameter <see cref="DataTable"/> column name for list values.
        /// </summary>
        public string TvpListValueColumnName { get; set; } = "Value";
    }
}