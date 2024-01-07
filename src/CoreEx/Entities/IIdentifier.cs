// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Text.Json.Serialization;

namespace CoreEx.Entities
{
    /// <summary>
    /// Enables the <see cref="Id"/> capability.
    /// </summary>
    public interface IIdentifier : IEntityKey
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        object? Id { get; set; }

        /// <summary>
        /// Gets the <see cref="Id"/> <see cref="Type"/>.
        /// </summary>
        [JsonIgnore]
        Type IdType { get; }

        /// <inheritdoc/>
        [JsonIgnore]
        CompositeKey IEntityKey.EntityKey => new(Id);
    }
}