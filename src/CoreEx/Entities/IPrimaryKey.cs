// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

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
        CompositeKey PrimaryKey { get; }

        /// <inheritdoc/>
        CompositeKey IEntityKey.EntityKey => PrimaryKey;
    }
}