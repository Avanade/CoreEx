// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Entities
{
    /// <summary>
    /// Enables the identifier (<see cref="Id"/>) capability.
    /// </summary>
    /// <typeparam name="T">The identifier <see cref="Type"/>.</typeparam>
    public interface IIdentifier<T> : IIdentifier
    {
        /// <inheritdoc/>
        object? IIdentifier.Id { get => Id; set => Id = (T)value!; }

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        new T Id { get; set; }
    }
}