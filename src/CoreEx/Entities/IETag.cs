// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Entities
{
    /// <summary>
    /// Provides the <see cref="ETag"/> property for the likes of versioning (optimistic concurrency).
    /// </summary>
    public interface IETag
    {
        /// <summary>
        /// Gets or sets the entity tag.
        /// </summary>
        string? ETag { get; set; }
    }
}