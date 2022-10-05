// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.RefData;
using System.Collections.Generic;
using System.Data;

namespace CoreEx.Database.Extended
{
    /// <summary>
    /// Represents the standard database column names.
    /// </summary>
    /// <remarks>These are used internally to map .NET properties to database column names.</remarks>
    public class DatabaseColumns
    {
        /// <summary>
        /// Gets or sets the <see cref="ChangeLog.CreatedDate"/> column name (defaults to <see cref="ChangeLog.CreatedDate"/>).
        /// </summary>
        public string CreatedDateName { get; set; } = nameof(ChangeLog.CreatedDate);

        /// <summary>
        /// Gets or sets the <see cref="ChangeLog.CreatedBy"/> column name (defaults to <see cref="ChangeLog.CreatedBy"/>).
        /// </summary>
        public string CreatedByName { get; set; } = nameof(ChangeLog.CreatedBy);

        /// <summary>
        /// Gets or sets the <see cref="ChangeLog.UpdatedDate"/> column name (defaults to <see cref="ChangeLog.UpdatedDate"/>).
        /// </summary>
        public string UpdatedDateName { get; set; } = nameof(ChangeLog.UpdatedDate);

        /// <summary>
        /// Gets or sets the <see cref="ChangeLog.UpdatedBy"/> column name (defaults to <see cref="ChangeLog.UpdatedBy"/>).
        /// </summary>
        public string UpdatedByName { get; set; } = nameof(ChangeLog.UpdatedBy);

        /// <summary>
        /// Gets or sets the '<c>ReselectRecord</c>' column name (defaults to '<c>ReselectRecord</c>'").
        /// </summary>
        public string ReselectRecordName { get; set; } = "ReselectRecord";

        /// <summary>
        /// Gets or sets the <see cref="PagingArgs.Skip"/> column name (defaults to '<c>PagingSkip</c>'").
        /// </summary>
        public string PagingSkipName { get; set; } = "PagingSkip";

        /// <summary>
        /// Gets or sets the <see cref="PagingArgs.Take"/> column name (defaults to '<c>PagingTake</c>'").
        /// </summary>
        public string PagingTakeName { get; set; } = "PagingTake";

        /// <summary>
        /// Gets or sets the <see cref="PagingArgs.IsGetCount"/> column name (defaults to '<c>PagingCount</c>'").
        /// </summary>
        public string PagingCountName { get; set; } = "PagingCount";

        /// <summary>
        /// Gets or sets the '<c>RowVersion</c>' column name (defaults to '<c>RowVersion</c>'").
        /// </summary>
        public string RowVersionName { get; set; } = "RowVersion";

        /// <summary>
        /// Gets or sets the <see cref="IETag.ETag"/> database column name (defaults to '<c>RowVersion</c>'").
        /// </summary>
        public string ETagName { get; set; } = "RowVersion";

        /// <summary>
        /// Gets or sets the '<c>ReturnValue</c>' column name.
        /// </summary>
        public string ReturnValueName { get; set; } = "ReturnValue";

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
        /// Gets or sets the <see cref="IReferenceData"/> <see cref="IIdentifier.Id"/> database column name (defaults to <see cref="IIdentifier.Id"/>).
        /// </summary>
        public string RefDataIdName { get; set; } = nameof(IReferenceData.Id);

        /// <summary>
        /// Gets or sets the <see cref="IReferenceData.Code"/> database column name (defaults to <see cref="IReferenceData.Code"/>).
        /// </summary>
        public string RefDataCodeName { get; set; } = nameof(IReferenceData.Code);

        /// <summary>
        /// Gets or sets the <see cref="IReferenceData.Text"/> database column name (defaults to <see cref="IReferenceData.Text"/>).
        /// </summary>
        public string RefDataTextName { get; set; } = nameof(IReferenceData.Text);

        /// <summary>
        /// Gets or sets the <see cref="IReferenceData.Description"/> database column name (defaults to <see cref="IReferenceData.Description"/>).
        /// </summary>
        public string RefDataDescriptionName { get; set; } = nameof(IReferenceData.Description);

        /// <summary>
        /// Gets or sets the <see cref="IReferenceData.SortOrder"/> database column name (defaults to <see cref="IReferenceData.SortOrder"/>).
        /// </summary>
        public string RefDataSortOrderName { get; set; } = nameof(IReferenceData.SortOrder);

        /// <summary>
        /// Gets or sets the <see cref="IReferenceData.IsActive"/> database column name (defaults to <see cref="IReferenceData.IsActive"/>).
        /// </summary>
        public string RefDataIsActiveName { get; set; } = nameof(IReferenceData.IsActive);

        /// <summary>
        /// Gets or sets the <see cref="IReferenceData.StartDate"/> database column name (defaults to <see cref="IReferenceData.StartDate"/>).
        /// </summary>
        public string RefDataStartDateName { get; set; } = nameof(IReferenceData.StartDate);

        /// <summary>
        /// Gets or sets the <see cref="IReferenceData.EndDate"/> database column name (defaults to <see cref="IReferenceData.EndDate"/>).
        /// </summary>
        public string RefDataEndDateName { get; set; } = nameof(IReferenceData.EndDate);

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