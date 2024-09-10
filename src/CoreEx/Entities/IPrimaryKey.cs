// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System.Text.Json.Serialization;

namespace CoreEx.Entities
{
    /// <summary>
    /// Provides the <see cref="PrimaryKey"/>.
    /// </summary>
    public interface IPrimaryKey : IEntityKey
    {
        /// <summary>
        /// Gets the <i>primary key</i> (represented as a <see cref="CompositeKey"/>).
        /// </summary>
        [JsonIgnore]
        CompositeKey PrimaryKey { get; }

        /// <inheritdoc/>
        [JsonIgnore]
        CompositeKey IEntityKey.EntityKey => PrimaryKey;
    }
}