// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Entities
{
    /// <summary>
    /// Provides the <see cref="ChangeLogAudit"/>.
    /// </summary>
    public interface IChangeLogAuditLog
    {
        /// <summary>
        /// Gets or set the <see cref="IChangeLogAudit"/> value.
        /// </summary>
        IChangeLogAudit? ChangeLogAudit { get; set; }
    }
}