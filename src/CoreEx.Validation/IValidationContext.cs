// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System;
using System.Collections.Generic;

namespace CoreEx.Validation
{
    /// <summary>
    /// Provides the validation context properties for an entity.
    /// </summary>
    public interface IValidationContext : IValidationResult
    {
        /// <summary>
        /// Gets the entity <see cref="Type"/>.
        /// </summary>
        Type EntityType { get; }

        /// <summary>
        /// Gets the entity prefix used for fully qualified <i>entity.property</i> naming (<c>null</c> represents the root).
        /// </summary>
        string? FullyQualifiedEntityName { get; }

        /// <summary>
        /// Gets the entity prefix used for fully qualified JSON <i>entity.property</i> naming (<c>null</c> represents the root).
        /// </summary>
        string? FullyQualifiedJsonEntityName { get; }

        /// <summary>
        /// Indicates whether JSON names were used for the <see cref="MessageItem"/> <see cref="MessageItem.Property"/>; by default (<c>false</c>) uses the .NET property names.
        /// </summary>
        bool UsedJsonNames { get; }

        /// <summary>
        /// Gets the configuration parameters.
        /// </summary>
        /// <remarks>Configuration parameters provide a means to pass values down through the validation stack. The consuming developer must instantiate the property on first use.</remarks>
        IDictionary<string, object?>? Config { get; }

        /// <summary>
        /// Determines whether one of the specified fully qualified property names has an error.
        /// </summary>
        /// <param name="fullyQualifiedPropertyName">The fully qualified property name.</param>
        /// <returns><c>true</c> where an error exists for at least one of the specified properties; otherwise, <c>false</c>.</returns>
        bool HasError(string fullyQualifiedPropertyName);
    }
}