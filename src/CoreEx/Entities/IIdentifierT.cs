// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Entities
{
    /// <summary>
    /// Enables the identifier (<see cref="Id"/>) capability.
    /// </summary>
    /// <typeparam name="TId">The identifier <see cref="Type"/>.</typeparam>
    /// <remarks>The <typeparamref name="TId"/> is contrained to <see cref="IComparable{T}"/> and <see cref="IEquatable{T}"/> to largely limit the <see cref="Id"/> to primitive types; e.g. <see cref="string"/>, <see cref="int"/>,
    /// <see cref="long"/> and <see cref="Guid"/>.</remarks>
    public interface IIdentifier<TId> : IIdentifier where TId : IComparable<TId>, IEquatable<TId>
    {
        /// <inheritdoc/>
        object? IIdentifier.Id { get => Id; set => Id = (TId)value!; }

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        new TId? Id { get; set; }
    }
}