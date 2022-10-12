// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Entities
{
    /// <summary>
    /// Provides base entity key support using a <see cref="CompositeKey"/>.
    /// </summary>
    /// <remarks>To enable key-based support in a consistent and standardized manner then this interface must be implemented; i.e. <see cref="IIdentifier"/> and <see cref="IPrimaryKey"/>.</remarks>
    public interface IEntityKey
    {
        /// <summary>
        /// Gets the key for the entity as a <see cref="CompositeKey"/>.
        /// </summary>
        /// <returns>The key represented as a <see cref="CompositeKey"/>.</returns>
        CompositeKey EntityKey { get; }
    }
}