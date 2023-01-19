// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities.Extended;
using CoreEx.Mapping;
using System;

namespace CoreEx.Database.Mapping
{
    /// <summary>
    /// Represents a <see cref="ChangeLogEx"/> <see cref="IDatabaseMapper"/>.
    /// </summary>
    public readonly struct ChangeLogExDatabaseMapper : IDatabaseMapper<ChangeLogEx>
    {
        private static readonly Lazy<ChangeLogExDatabaseMapper> _default = new(() => new(), true);

        /// <summary>
        /// Gets the default (singleton) instance.
        /// </summary>
        public static ChangeLogExDatabaseMapper Default => _default.Value;

        /// <inheritdoc/>
        public ChangeLogEx? MapFromDb(DatabaseRecord record, OperationTypes operationType = OperationTypes.Unspecified)
        {
            if (OperationTypes.AnyExceptGet.HasFlag(operationType))
            {
                var ChangeLogEx = new ChangeLogEx
                {
                    CreatedBy = record.GetValue<string?>(record.Database.DatabaseColumns.CreatedByName),
                    CreatedDate = record.GetValue<DateTime?>(record.Database.DatabaseColumns.CreatedDateName),
                    UpdatedBy = record.GetValue<string?>(record.Database.DatabaseColumns.UpdatedByName),
                    UpdatedDate = record.GetValue<DateTime?>(record.Database.DatabaseColumns.UpdatedDateName)
                };

                return ((Entities.IChangeLogAudit)ChangeLogEx).IsInitial ? null : ChangeLogEx;
            }

            return null;
        }

        /// <inheritdoc/>
        public void MapToDb(ChangeLogEx? value, DatabaseParameterCollection parameters, OperationTypes operationType = OperationTypes.Unspecified)
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