// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Entities
{
    /// <summary>
    /// Provides the <see cref="TenantId"/>.
    /// </summary>
    public interface ITenantId
    {
        /// <summary>
        /// Gets or sets the tenant identifier.
        /// </summary>
        string? TenantId { get; set; }
    }
}