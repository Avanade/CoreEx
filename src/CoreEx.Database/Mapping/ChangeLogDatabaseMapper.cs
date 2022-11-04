// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Mapping;
using System;

namespace CoreEx.Database.Mapping
{
    /// <summary>
    /// Represents a <see cref="ChangeLog"/> <see cref="IDatabaseMapper"/>.
    /// </summary>
    public struct ChangeLogDatabaseMapper : IDatabaseMapper<ChangeLog>
    {
        private static readonly Lazy<ChangeLogDatabaseMapper> _default = new(() => new(), true);

        /// <summary>
        /// Gets the default (singleton) instance.
        /// </summary>
        public static ChangeLogDatabaseMapper Default => _default.Value;

        /// <inheritdoc/>
        public ChangeLog? MapFromDb(DatabaseRecord record, OperationTypes operationType = OperationTypes.Unspecified)
        {
            if (OperationTypes.AnyExceptGet.HasFlag(operationType))
            {
                var changeLog = new ChangeLog
                {
                    CreatedBy = record.GetValue<string?>(record.Database.DatabaseColumns.CreatedByName),
                    CreatedDate = record.GetValue<DateTime?>(record.Database.DatabaseColumns.CreatedDateName),
                    UpdatedBy = record.GetValue<string?>(record.Database.DatabaseColumns.UpdatedByName),
                    UpdatedDate = record.GetValue<DateTime?>(record.Database.DatabaseColumns.UpdatedDateName)
                };

                return changeLog.IsInitial ? null : changeLog;
            }

            return null;
        }

        /// <inheritdoc/>
        public void MapToDb(ChangeLog? value, DatabaseParameterCollection parameters, OperationTypes operationType = OperationTypes.Unspecified)
        {
            if (value == null || !parameters.Database.EnableChangeLogMapperToDb)
                return;

            if (OperationTypes.AnyExceptUpdate.HasFlag(operationType))
            {
                parameters.Param(parameters.Database.DatabaseColumns.CreatedByName, value.CreatedBy);
                parameters.Param(parameters.Database.DatabaseColumns.CreatedDateName, value.CreatedDate);
            }

            if (OperationTypes.AnyExceptCreate.HasFlag(operationType))
            {
                parameters.Param(parameters.Database.DatabaseColumns.UpdatedByName, value.UpdatedBy);
                parameters.Param(parameters.Database.DatabaseColumns.UpdatedDateName, value.UpdatedDate);
            }
        }
    }
}