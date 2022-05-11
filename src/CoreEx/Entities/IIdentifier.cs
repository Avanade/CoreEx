// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Entities
{
    /// <summary>
    /// Enables the <see cref="Id"/> capability.
    /// </summary>
    public interface IIdentifier
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        object? Id { get; set; }

        /// <summary>
        /// Gets the <see cref="Id"/> <see cref="Type"/>.
        /// </summary>
        Type IdType { get; }
    }
}