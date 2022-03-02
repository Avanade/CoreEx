// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Entities
{
    /// <summary>
    /// Provides the <see cref="PartitionKey"/>.
    /// </summary>
    public interface IPartitionKey
    {
        /// <summary>
        /// Gets the partition key.
        /// </summary>
        public string? PartitionKey { get; }
    }
}