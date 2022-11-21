// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Entities.Extended
{
    /// <summary>
    /// Provides an extended <see cref="EntityBase"/> <see cref="ChangeLog"/>.
    /// </summary>
    public interface IChangeLogEx : IChangeLogAuditLog
    {
        /// <inheritdoc/>
        IChangeLogAudit? IChangeLogAuditLog.ChangeLogAudit
        {
            get => ChangeLog;
            set
            {
                if (value is null)
                    ChangeLog = null;
                else if (value is ChangeLogEx cl)
                    ChangeLog = cl;
                else
                {
                    ChangeLog = new ChangeLogEx
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
        ChangeLogEx? ChangeLog { get; set; }
    }
}