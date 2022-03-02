// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Entities
{
    /// <summary>
    /// Enables an entity to identify whether it is logically deleted.
    /// </summary>
    public interface ILogicallyDeleted
    {
        /// <summary>
        /// Indicates whether the entity is considered logically deleted.
        /// </summary>
        bool? IsDeleted { get; set; }
    }
}