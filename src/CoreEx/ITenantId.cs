// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx
{
    /// <summary>
    /// Provides the <see cref="TenantId"/>.
    /// </summary>
    public interface ITenantId
    {
        /// <summary>
        /// Gets the partition key.
        /// </summary>
        public string? TenantId { get; }
    }
}