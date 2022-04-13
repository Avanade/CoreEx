// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Concurrent;

namespace CoreEx.Abstractions.Reflection
{
    /// <summary>
    /// Provides the arguments passed to and through a <see cref="EntityReflector"/>.
    /// </summary>
    public class EntityReflectorArgs
    {
        /// <summary>
        /// Initializes an instance of the <see cref="EntityReflectorArgs"/> class with an optional <paramref name="cache"/>.
        /// </summary>
        /// <param name="cache">The <b>cache</b> <see cref="ConcurrentDictionary{Type, IEntityReflector}"/> to use versus instantiating each <see cref="EntityReflector"/> per use.</param>
        public EntityReflectorArgs(ConcurrentDictionary<Type, IEntityReflector>? cache = null) => Cache = cache ?? new();

        /// <summary>
        /// Gets the <b>cache</b> <see cref="ConcurrentDictionary{Type, IEntityReflector}"/> to use versus instantiating each <see cref="EntityReflector"/> per use.
        /// </summary>
        public ConcurrentDictionary<Type, IEntityReflector> Cache { get; }

        /// <summary>
        /// Gets or sets the action to invoke to perform additional logic when reflecting/building the <b>entity</b> <see cref="Type"/>.
        /// </summary>
        public Action<IEntityReflector>? EntityBuilder { get; set; } = null;

        /// <summary>
        /// Indicates whether to automatically populate the entity properties using the optional <see cref="PropertyBuilder"/> (defaults to <c>true</c>).
        /// </summary>
        public bool AutoPopulateProperties { get; set; } = true;

        /// <summary>
        /// Gets or sets the function to invoke to perform additional logic when reflecting/building the <b>property</b> <see cref="Type"/>; the result determines whether the
        /// property should be included (<c>true</c>) or not (<c>false</c>) within the underlying properties collection.
        /// </summary>
        public Func<IPropertyReflector, bool>? PropertyBuilder { get; set; } = null;

        /// <summary>
        /// Defines the <see cref="StringComparer"/> for finding the property/JSON names (defaults to <see cref="StringComparer.Ordinal"/>).
        /// </summary>
        public StringComparer NameComparer { get; set; } = StringComparer.Ordinal;
    }
}