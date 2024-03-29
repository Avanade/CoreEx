﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Entities
{
    /// <summary>
    /// Provides the <see cref="ChangeLog"/>.
    /// </summary>
    public interface IChangeLog : IChangeLogAuditLog
    {
        /// <inheritdoc/>
        IChangeLogAudit? IChangeLogAuditLog.ChangeLogAudit
        { 
            get => ChangeLog;
            set
            {
                if (value is null)
                    ChangeLog = null;
                else if (value is ChangeLog cl)
                    ChangeLog = cl;
                else
                {
                    ChangeLog = new ChangeLog
                    {
                        CreatedBy = value.CreatedBy,
                        CreatedDate = value.CreatedDate,
                        UpdatedBy = value.UpdatedBy,
                        UpdatedDate = value.UpdatedDate
                    };
                }
            }
        }

        /// <summary>
        /// Gets or set the <see cref="ChangeLog"/>.
        /// </summary>
        ChangeLog? ChangeLog { get; set; }
    }
}