// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Abstractions.Reflection
{
    /// <summary>
    /// Enables a reflector for a given entity (class) <see cref="Type"/>.
    /// </summary>
    public interface IEntityReflector
    {
        /// <summary>
        /// Gets the entity <see cref="Type"/>.
        /// </summary>
        Type Type { get; }

        /// <summary>
        /// Gets the <see cref="EntityReflectorArgs"/>.
        /// </summary>
        EntityReflectorArgs Args { get; }

        /// <summary>
        /// Gets the <see cref="IPropertyReflector"/> for the specified property name.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <returns>The <see cref="IPropertyReflector"/>.</returns>
        IPropertyReflector GetProperty(string name);

        /// <summary>
        /// Gets the <see cref="IPropertyReflector"/> for the specified JSON name.
        /// </summary>
        /// <param name="jsonName">The JSON name.</param>
        /// <returns>The <see cref="IPropertyReflector"/>.</returns>
        IPropertyReflector? GetJsonProperty(string jsonName);
    }
}